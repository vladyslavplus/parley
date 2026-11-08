using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Parley.Api.Health;

/// <summary>
/// Verifies that PostgreSQL answers a trivial query.
/// </summary>
public sealed class DatabaseHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    private static readonly string ProbeSql = "SELECT 1";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = dataSource.CreateCommand(ProbeSql);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException or TimeoutException)
        {
            return HealthCheckResult.Unhealthy("Database is unreachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
