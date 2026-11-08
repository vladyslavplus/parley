using Microsoft.Extensions.Options;
using Parley.Api.Hangfire;
using Parley.Api.Options;
using Parley.SharedKernel.Configuration;
using Parley.SharedKernel.Persistence.Concurrency;

namespace Parley.Api.Startup;

public static class PlatformOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates the platform options (database, telemetry, trusted proxies, retry, Hangfire).
    /// </summary>
    public static IServiceCollection AddPlatformOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(options => ConfigurationPlaceholders.IsSet(options.ConnectionString), "Database connection string is missing or still a placeholder. Set Database__ConnectionString.")
            .ValidateOnStart();

        services.AddOptions<TelemetryOptions>()
            .Bind(configuration.GetSection(TelemetryOptions.SectionName))
            .Validate(options => ConfigurationPlaceholders.IsSet(options.OtlpEndpoint) && Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out _), "Telemetry OTLP endpoint must be an absolute URI and must not be a placeholder. Set Telemetry__OtlpEndpoint.")
            .ValidateOnStart();

        services.AddOptions<TrustedProxyOptions>()
            .Bind(configuration.GetSection(TrustedProxyOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<TrustedProxyOptions>, TrustedProxyOptionsValidator>();

        services.AddOptions<DatabaseRetryOptions>()
            .Bind(configuration.GetSection(DatabaseRetryOptions.SectionName))
            .Validate(options => options.MaxAttempts is >= DatabaseRetryOptions.MinimumAttempts and <= DatabaseRetryOptions.MaximumAttempts, "Database retry attempts are out of range.")
            .Validate(options => options.MaxRetryDelay >= TimeSpan.Zero && options.MaxRetryDelay <= DatabaseRetryOptions.MaximumAllowedRetryDelay, "Database retry backoff is out of range.")
            .ValidateOnStart();

        services.AddOptions<HangfireOptions>()
            .Bind(configuration.GetSection(HangfireOptions.SectionName))
            .Validate(options => options.WorkerCount is >= HangfireOptions.MinimumWorkerCount and <= HangfireOptions.MaximumWorkerCount, "Hangfire worker count is out of range.")
            .Validate(options => options.Queues.All(queue => !string.IsNullOrWhiteSpace(queue)), "Hangfire queues must not be empty names.")
            .Validate(options => options.JobExpiration > TimeSpan.Zero, "Hangfire job expiration must be positive.")
            .Validate(options => options.JobExpirationCheckInterval > TimeSpan.Zero, "Hangfire expiration check interval must be positive.")
            .Validate(options => options.DeleteExpiredBatchSize is >= HangfireOptions.MinimumDeleteExpiredBatchSize and <= HangfireOptions.MaximumDeleteExpiredBatchSize, "Hangfire delete-expired batch size is out of range.")
            .Validate(options => options.RetryAttempts is >= HangfireOptions.MinimumRetryAttempts and <= HangfireOptions.MaximumRetryAttempts, "Hangfire retry attempts are out of range.")
            .Validate(options => options.QueuePollInterval > TimeSpan.Zero, "Hangfire queue poll interval must be positive.")
            .ValidateOnStart();
        return services;
    }
}
