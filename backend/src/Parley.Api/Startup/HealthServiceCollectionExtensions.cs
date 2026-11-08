using Parley.Api.Health;

namespace Parley.Api.Startup;

public static class HealthServiceCollectionExtensions
{
    /// <summary>
    /// Registers the health checks.
    /// </summary>
    public static IServiceCollection AddHealthEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<DatabaseHealthCheck>();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>(ApiConstants.DatabaseHealthCheckName);
        return services;
    }
}
