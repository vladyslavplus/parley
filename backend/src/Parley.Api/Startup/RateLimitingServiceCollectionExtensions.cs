using Parley.Api.RateLimiting;

namespace Parley.Api.Startup;

public static class RateLimitingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the authentication rate limiting policies.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddAuthRateLimiting();
        return services;
    }
}
