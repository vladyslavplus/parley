using Microsoft.Extensions.DependencyInjection;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.SharedKernel.Tenancy.Implementations;

/// <summary>
/// Registers tenancy for both HTTP requests and background work. The host must
/// also register <c>services.AddHttpContextAccessor()</c> (an ASP.NET Core
/// service), which is why it is not registered here. Module DbContexts take
/// <see cref="ITenantContext"/> from DI (the composite above) and are therefore
/// scoped per request or job. Job scopes set the tenant through
/// <see cref="IMutableTenantContext"/> (backed by <see cref="JobTenantContext"/>)
/// before the first query.
/// </summary>
public static class TenantServiceCollectionExtensions
{
    public static IServiceCollection AddTenancy(this IServiceCollection services)
    {
        services.AddSingleton<JobTenantContext>();
        services.AddSingleton<IMutableTenantContext>(provider => provider.GetRequiredService<JobTenantContext>());
        services.AddSingleton<JobCurrentUser>();
        services.AddScoped<ITenantContext, CompositeTenantContext>();
        services.AddScoped<ICurrentUser, CompositeCurrentUser>();
        return services;
    }
}
