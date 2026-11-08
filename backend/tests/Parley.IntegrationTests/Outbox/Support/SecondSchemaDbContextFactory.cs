using Microsoft.EntityFrameworkCore;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Persistence.Interceptors;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Builds second-schema contexts with the same production conventions as the
/// tenancy context factory.
/// </summary>
public static class SecondSchemaDbContextFactory
{
    public static DbContextOptions<SecondSchemaDbContext> CreateOptions(
        string connectionString,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        DatabaseRetryOptions? retryOptions = null)
    {
        var retry = retryOptions ?? new DatabaseRetryOptions();
        var builder = new DbContextOptionsBuilder<SecondSchemaDbContext>();
        builder.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            retry.MaxAttempts,
            retry.MaxRetryDelay,
            (ICollection<string>)PersistenceConstants.PostgreSqlErrorCodesToRetry));
        builder.AddTenantConventions();
        builder.AddInterceptors(new AuditStampingInterceptor(timeProvider, tenantContext));
        return builder.Options;
    }

    public static SecondSchemaDbContext Create(
        string connectionString,
        ITenantContext tenantContext,
        TimeProvider timeProvider,
        DatabaseRetryOptions? retryOptions = null) =>
        new(CreateOptions(connectionString, tenantContext, timeProvider, retryOptions), tenantContext);
}
