using Hangfire.PostgreSql;
using Npgsql;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Api.Hangfire;

/// <summary>
/// Installs the Hangfire storage schema and its autovacuum tuning, once per database, under an advisory lock so concurrent
/// starts do not race. The API host calls it before the background server starts.
/// </summary>
public static class HangfireSchemaInstaller
{
    public static async Task InstallAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using (var acquire = new NpgsqlCommand("SELECT pg_advisory_lock(@p0)", connection))
        {
            acquire.Parameters.AddWithValue("p0", HangfireConstants.SchemaInstallLockKey);
            await acquire.ExecuteNonQueryAsync(cancellationToken);
        }

        try
        {
            PostgreSqlObjectsInstaller.Install(connection, PersistenceConstants.HangfireSchema);
            foreach (var statement in HangfirePostInitSql.AutovacuumStatements)
            {
                await using var command = new NpgsqlCommand(statement, connection);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            await using var release = new NpgsqlCommand("SELECT pg_advisory_unlock(@p0)", connection);
            release.Parameters.AddWithValue("p0", HangfireConstants.SchemaInstallLockKey);
            await release.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
