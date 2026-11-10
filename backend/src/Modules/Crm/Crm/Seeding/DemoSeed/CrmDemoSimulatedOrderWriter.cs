using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Parley.Crm.Contracts.DemoSeed;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// The one writer of <c>demo_simulated</c> orders. Its guard is the database and the environment, checked before any row:
/// the Demo environment, or Development against a database whose name is <c>parley_demo</c> or <c>parley_demo_&lt;suffix&gt;</c>.
/// The database check is the real protection; the environment check keeps the writer out of production hosts even if a
/// database were misnamed.
/// </summary>
public sealed class CrmDemoSimulatedOrderWriter(NpgsqlDataSource dataSource, IHostEnvironment environment) : ICrmDemoSimulatedOrderWriter
{
    private const string DemoEnvironmentName = "Demo";

    private const string DemoDatabaseNamePattern = "^parley_demo(_[a-z0-9_]+)?\\z";

    private const string OrganizationParameter = "org";

    private const string TimeParameter = "now";

    private const string JsonParameter = "orders";

    private const string CurrencyParameter = "currency";

    private static readonly Regex DemoDatabaseName = new(DemoDatabaseNamePattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private const string InsertStatement = """
        INSERT INTO crm.orders
            (id, organization_id, contact_id, external_ref, ordered_at, total_amount, currency, line_count,
             is_cancellation, source, created_at, updated_at)
        SELECT gen_random_uuid(), @org, c.id, s.external_ref, s.ordered_at, s.total_amount, @currency, 1,
               false, 'demo_simulated', @now, @now
        FROM jsonb_to_recordset(CAST(@orders AS jsonb)) AS s(contact_id uuid, external_ref text, ordered_at timestamptz, total_amount numeric)
        JOIN crm.contacts c ON c.id = s.contact_id AND c.organization_id = @org
        ON CONFLICT (organization_id, source, external_ref) DO NOTHING
        """;

    private const string LastOrderStatement = """
        UPDATE crm.contacts c
        SET last_order_at = x.last_order_at
        FROM (
            SELECT o.contact_id, MAX(o.ordered_at) AS last_order_at
            FROM crm.orders o
            WHERE o.organization_id = @org AND NOT o.is_cancellation
              AND o.contact_id IN (SELECT s.contact_id FROM jsonb_to_recordset(CAST(@orders AS jsonb)) AS s(contact_id uuid))
            GROUP BY o.contact_id) x
        WHERE c.organization_id = @org AND c.id = x.contact_id AND c.last_order_at IS DISTINCT FROM x.last_order_at
        """;

    public async Task<int> WriteAsync(Guid organizationId, IReadOnlyList<CrmSimulatedOrder> orders, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(orders);
        EnsureDemoTarget();
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
            insert.Parameters.AddWithValue(CurrencyParameter, CrmDemoContract.ExportCurrency);
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

    /// <summary>Refuses any target that is not a demo database of a demo environment. Nothing is written before this passes.</summary>
    private void EnsureDemoTarget()
    {
        var builder = new NpgsqlConnectionStringBuilder(dataSource.ConnectionString);
        var isDemoEnvironment = environment.IsEnvironment(DemoEnvironmentName) || environment.IsDevelopment();
        if (!isDemoEnvironment)
        {
            throw new InvalidOperationException("Simulated demo orders are written only in the Demo or Development environment.");
        }

        if (string.IsNullOrEmpty(builder.Database) || !DemoDatabaseName.IsMatch(builder.Database))
        {
            throw new InvalidOperationException(
                "Simulated demo orders are written only to a database named parley_demo or parley_demo_<suffix>.");
        }
    }

    private sealed record SimulatedRow(Guid ContactId, string ExternalRef, DateTimeOffset OrderedAt, decimal TotalAmount);
}
