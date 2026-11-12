using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Domain.Orders;
using Parley.Crm.Persistence;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// The one writer of <c>demo_simulated</c> orders. Its guard is the host environment, checked before any row: only a
/// Development host may write simulated orders. The demo organization is not production data, so the environment, not the
/// database name, is the protection.
/// </summary>
public sealed class CrmDemoSimulatedOrderWriter(NpgsqlDataSource dataSource, IHostEnvironment environment) : ICrmDemoSimulatedOrderWriter
{
    private const string OrganizationParameter = "org";

    private const string TimeParameter = "now";

    private const string JsonParameter = "orders";

    private const string CurrencyParameter = "currency";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static readonly string InsertStatement = $$"""
        INSERT INTO "{{CrmSchemaNames.Schema}}"."{{CrmSchemaNames.OrdersTable}}"
            (id, organization_id, contact_id, external_ref, ordered_at, total_amount, currency, line_count,
             is_cancellation, source, created_at, updated_at)
        SELECT gen_random_uuid(), @org, c.id, s.external_ref, s.ordered_at, s.total_amount, @currency, 1,
               false, '{{OrderSources.DemoSimulated}}', @now, @now
        FROM jsonb_to_recordset(CAST(@orders AS jsonb)) AS s(contact_id uuid, external_ref text, ordered_at timestamptz, total_amount numeric)
        JOIN "{{CrmSchemaNames.Schema}}"."{{CrmSchemaNames.ContactsTable}}" c ON c.id = s.contact_id
        WHERE c.organization_id = @org
        ON CONFLICT (organization_id, source, external_ref) DO NOTHING
        """;

    private static readonly string LastOrderStatement = $$"""
        UPDATE "{{CrmSchemaNames.Schema}}"."{{CrmSchemaNames.ContactsTable}}" c
        SET last_order_at = x.last_order_at
        FROM (
            SELECT o.contact_id, MAX(o.ordered_at) AS last_order_at
            FROM "{{CrmSchemaNames.Schema}}"."{{CrmSchemaNames.OrdersTable}}" o
            WHERE o.organization_id = @org AND NOT o.is_cancellation
              AND o.contact_id IN (SELECT s.contact_id FROM jsonb_to_recordset(CAST(@orders AS jsonb)) AS s(contact_id uuid))
            GROUP BY o.contact_id) x
        WHERE c.organization_id = @org AND c.id = x.contact_id AND c.last_order_at IS DISTINCT FROM x.last_order_at
        """;

    public async Task<int> WriteAsync(Guid organizationId, IReadOnlyList<CrmSimulatedOrder> orders, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(orders);
        EnsureDevelopment();
        if (orders.Count == 0)
        {
            return 0;
        }

        var json = JsonSerializer.Serialize(orders.Select(order => new SimulatedRow(
            order.ContactId,
            order.ExternalRef,
            order.OrderedAt,
            order.TotalAmount)), JsonOptions);
        var now = DateTimeOffset.UtcNow;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        int inserted;
        await using (var insert = new NpgsqlCommand(InsertStatement, connection, transaction))
        {
            insert.Parameters.AddWithValue(OrganizationParameter, organizationId);
            insert.Parameters.AddWithValue(CurrencyParameter, CrmDemoContract.DemoCurrency);
            insert.Parameters.AddWithValue(TimeParameter, now);
            insert.Parameters.AddWithValue(JsonParameter, json);
            inserted = await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var lastOrder = new NpgsqlCommand(LastOrderStatement, connection, transaction))
        {
            lastOrder.Parameters.AddWithValue(OrganizationParameter, organizationId);
            lastOrder.Parameters.AddWithValue(JsonParameter, json);
            await lastOrder.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return inserted;
    }

    /// <summary>Refuses every host that is not Development. Nothing is written before this passes.</summary>
    private void EnsureDevelopment()
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Simulated demo orders are written only in the Development environment.");
        }
    }

    private sealed record SimulatedRow(Guid ContactId, string ExternalRef, DateTimeOffset OrderedAt, decimal TotalAmount);
}
