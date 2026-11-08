using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.SharedKernel.Idempotency.Abstractions;

namespace Parley.SharedKernel.Idempotency.Persistence;

public static class IdempotencyServiceCollectionExtensions
{
    /// <summary>Registers the store of one module, bound to that module's DbContext.</summary>
    public static IServiceCollection AddIdempotencyStore<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IModuleIdempotencyStore<TDbContext>, EfIdempotencyStore<TDbContext>>();
        return services;
    }
}
