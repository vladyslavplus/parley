using Microsoft.EntityFrameworkCore;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Outbox.Mapping;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Tenancy.Support;

/// <summary>
/// Test-only DbContext proving tenant isolation and the 409 path.
/// Scoped per test; never shared across parallel operations.
/// </summary>
public sealed class TenantTestDbContext(
    DbContextOptions<TenantTestDbContext> options,
    ITenantContext tenantContext) : DbContext(options), ITenantDbContext
{
    public const string SchemaName = "test_tenancy";

    public DbSet<TenantRecord> TenantRecords => Set<TenantRecord>();

    public ITenantContext TenantContext { get; } = tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.Entity<TenantRecord>();
        modelBuilder.Entity<TenantRecord>().HasTenantIndex(
            "ix_tenant_records_organization_id_name",
            nameof(TenantRecord.Name));
        modelBuilder.AddTenantConventions(this);
        modelBuilder.AddOutboxConventions();
        modelBuilder.AddIdempotencyConventions();
    }
}
