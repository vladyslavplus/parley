using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using Parley.Api.Hangfire;
using Parley.Api.Options;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Api.Startup;

public static class BackgroundProcessingServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hangfire storage, the job server and the dashboard filter. With <paramref name="runJobs"/> false the
    /// recurring jobs are not registered and no job server starts; the storage stays available for enqueueing.
    /// </summary>
    public static IServiceCollection AddBackgroundProcessing(this IServiceCollection services, bool runJobs = true)
    {
        services.AddSingleton<PlatformOperatorDashboardAuthorizationFilter>();

        services.AddHangfire((provider, configuration) =>
        {
            var hangfireOptions = provider.GetRequiredService<IOptions<HangfireOptions>>().Value;
            var connectionString = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString;
            configuration
                .UsePostgreSqlStorage(bootstrapper => bootstrapper.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                {
                    SchemaName = PersistenceConstants.HangfireSchema,
                    PrepareSchemaIfNecessary = false,
                    QueuePollInterval = hangfireOptions.QueuePollInterval,
                    JobExpirationCheckInterval = hangfireOptions.JobExpirationCheckInterval,
                    DeleteExpiredBatchSize = hangfireOptions.DeleteExpiredBatchSize,
                })
                .WithJobExpirationTimeout(hangfireOptions.JobExpiration)
                .UseFilter(new AutomaticRetryAttribute
                {
                    Attempts = hangfireOptions.RetryAttempts,
                    OnAttemptsExceeded = AttemptsExceededAction.Delete,
                });
            configuration.UseSerilogLogProvider();
        });

        // The schema initializer runs before the server starts so the storage tables
        // exist by the first poll. Modules opting into [IdempotencyKey] resolve their
        // scoped store (EfIdempotencyStore<TDbContext>) plus IdempotencyKeyFilter;
        // module hosts register both.
        if (!runJobs)
        {
            return services;
        }

        services.AddHostedService<HangfireSchemaInitializer>();
        services.AddHostedService(provider =>
        {
            var hangfireOptions = provider.GetRequiredService<IOptions<HangfireOptions>>().Value;
            var server = new BackgroundJobServer(
                new BackgroundJobServerOptions
                {
                    WorkerCount = hangfireOptions.WorkerCount,
                    Queues = hangfireOptions.Queues.Length > 0
                        ? hangfireOptions.Queues
                        : [HangfireOptions.DefaultQueueName],
                },
                provider.GetRequiredService<JobStorage>());
            return new BackgroundProcessingServerHostedService(server);
        });
        return services;
    }
}
