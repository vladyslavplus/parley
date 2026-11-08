using Microsoft.EntityFrameworkCore;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Persistence.Interceptors;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Tenancy.Support;

/// <summary>
/// Builds test contexts exactly like production registrations: Npgsql with the
/// bounded retry policy from <see cref="DatabaseRetryOptions"/>, snake_case
/// naming, the audit interceptor and tenant conventions.
/// </summary>
public static class TenantTestDbContextFactory
{
    public static DbContextOptions<TenantTestDbContext> CreateOptions(
        string connectionString,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        DatabaseRetryOptions? retryOptions = null)
    {
        var retry = retryOptions ?? new DatabaseRetryOptions();
        var builder = new DbContextOptionsBuilder<TenantTestDbContext>();
        builder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            retry.MaxAttempts,
            retry.MaxRetryDelay,
            (ICollection<string>)PersistenceConstants.PostgreSqlErrorCodesToRetry));
        builder.AddTenantConventions();
        builder.AddInterceptors(new AuditStampingInterceptor(timeProvider, tenantContext));
        return builder.Options;
    }

    public static TenantTestDbContext Create(
        string connectionString,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        DatabaseRetryOptions? retryOptions = null) =>
        new(CreateOptions(connectionString, tenantContext, timeProvider, retryOptions), tenantContext);
}
