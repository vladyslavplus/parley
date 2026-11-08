using Parley.SharedKernel.Mediator.Transactions;
using Parley.SharedKernel.Persistence.Interceptors;
using Parley.SharedKernel.Tenancy.Implementations;

namespace Parley.Api.Startup;

public static class TransactionsAndTenancyServiceCollectionExtensions
{
    /// <summary>
    /// Registers transaction scope, request context, tenancy and the audit interceptor.
    /// </summary>
    public static IServiceCollection AddTransactionsAndTenancy(this IServiceCollection services)
    {
        services.AddSingleton<ITransactionScopeFactory, NoOpTransactionScopeFactory>();
        services.AddHttpContextAccessor();
        services.AddTenancy();
        services.AddScoped<AuditStampingInterceptor>();
        return services;
    }
}
