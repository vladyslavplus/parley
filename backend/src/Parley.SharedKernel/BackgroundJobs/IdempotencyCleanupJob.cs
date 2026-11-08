using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Npgsql;
using Parley.SharedKernel.Idempotency;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.SharedKernel.BackgroundJobs;

/// <summary>
/// Recurring cleanup for <c>idempotency_keys</c> tables: deletes rows whose
/// expiry passed. The job is platform-wide (it sweeps every configured module
/// schema regardless of organization) and idempotent: deleting already-expired
/// rows twice is a no-op the second time.
///
/// Module schemas come from <see cref="IdempotencyOptions.CleanupSchemas"/>.
/// With no schemas configured the job is a deliberate no-op, so hosts without
/// module tables keep it registered and harmless.
/// </summary>
public sealed class IdempotencyCleanupJob(
    NpgsqlDataSource dataSource,
    IOptions<IdempotencyOptions> options,
    TimeProvider timeProvider,
    IMutableTenantContext tenantContext) : TenantBackgroundJob(tenantContext)
{
    private static readonly Regex SchemaNamePattern = new(
        "^[a-z_][a-z0-9_]*$",
        RegexOptions.CultureInvariant);

    protected override bool PlatformWide => true;

    /// <summary>Deletes expired idempotency rows in every configured schema.</summary>
    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var schemas = options.Value.CleanupSchemas;
        if (schemas.Length == 0)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        foreach (var schema in schemas)
        {
            if (!SchemaNamePattern.IsMatch(schema))
            {
                throw new InvalidOperationException(
                    $"Idempotency cleanup schema '{schema}' is not a valid identifier.");
            }

            await using var command = new NpgsqlCommand(
                $"DELETE FROM \"{schema}\".\"{IdempotencyTableNames.IdempotencyKeys}\" WHERE \"expires_at\" <= @p0",
                connection);
            command.Parameters.AddWithValue("p0", now);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
