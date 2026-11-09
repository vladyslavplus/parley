using Parley.Api.Idempotency;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Idempotency;

namespace Parley.Api.Startup;

public static class IdempotencyServiceCollectionExtensions
{
    /// <summary>
    /// Binds and validates the idempotency options.
    /// </summary>
    public static IServiceCollection AddIdempotency(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IdempotencyKeyFilter<CrmDbContext>>();
        services.AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .Validate(options => options.KeyLifetime > IdempotencyOptions.ZeroLifetime && options.KeyLifetime <= IdempotencyOptions.MaximumLifetime, "Idempotency key lifetime is out of range.")
            .Validate(options => options.MaxRequestBodyBytes is >= IdempotencyOptions.MinimumMaxBodyBytes and <= IdempotencyOptions.MaximumAllowedBodyBytes, "Idempotency body size limit is out of range.")
            .Validate(options => options.InProgressPollInterval >= TimeSpan.FromMilliseconds(IdempotencyOptions.MinimumPollIntervalMs), "Idempotency poll interval is too small.")
            .Validate(options => options.InProgressWaitTimeout >= TimeSpan.FromMilliseconds(IdempotencyOptions.MinimumWaitTimeoutMs) && options.InProgressWaitTimeout <= TimeSpan.FromMilliseconds(IdempotencyOptions.MaximumWaitTimeoutMs), "Idempotency wait timeout is out of range.")
            .ValidateOnStart();
        return services;
    }
}
