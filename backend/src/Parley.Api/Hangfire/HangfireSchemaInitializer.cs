using Hangfire;
using Hangfire.Common;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using Npgsql;
using Parley.Api.Options;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Api.Hangfire;

/// <summary>
/// One-time post-init for Hangfire storage. Hangfire owns its tables (they are
/// not EF-managed), so this hosted service: installs the storage schema
/// idempotently, applies the autovacuum tuning from
/// <see cref="HangfirePostInitSql"/>, and declares the recurring idempotency
/// cleanup job. It runs before the background server starts and retries on
/// transient failures without failing the host, so an unavailable database
/// never takes the API down.
/// </summary>
public sealed class HangfireSchemaInitializer(
    IOptions<DatabaseOptions> databaseOptions,
    IRecurringJobManager recurringJobs,
    TimeProvider timeProvider,
    ILogger<HangfireSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                await InstallAsync(cancellationToken);
                break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                attempts++;
                if (attempts >= HangfireConstants.SchemaInitMaxAttempts)
                {
                    logger.LogError(
                        exception,
                        "Hangfire schema initialization failed after {Attempts} attempts. The background server will retry fetching jobs.",
                        attempts);
                    break;
                }

                logger.LogWarning(
                    exception,
                    "Hangfire schema initialization failed (attempt {Attempts} of {MaxAttempts}). Retrying.",
                    attempts,
                    HangfireConstants.SchemaInitMaxAttempts);
                await Task.Delay(HangfireOptions.DefaultSchemaInitRetryDelay, timeProvider, cancellationToken);
            }
        }

        try
        {
            recurringJobs.AddOrUpdate(
                HangfireConstants.IdempotencyCleanupJobName,
                Job.FromExpression<IdempotencyCleanupJob>(job => job.ExecuteAsync(null, CancellationToken.None)),
                HangfireConstants.IdempotencyCleanupCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Recurring idempotency cleanup registration was deferred until the database is available.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task InstallAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(databaseOptions.Value.ConnectionString);
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
