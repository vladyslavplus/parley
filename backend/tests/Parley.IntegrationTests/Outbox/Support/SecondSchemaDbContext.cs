using Microsoft.EntityFrameworkCore;
using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.Outbox.Mapping;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Test-only second module DbContext in a separate schema. Proves the
/// dispatcher commits cross-module consumer work in the consumer schema
/// instead of silently dropping it.
/// </summary>
public sealed class SecondSchemaDbContext(
    DbContextOptions<SecondSchemaDbContext> options,
    ITenantContext tenantContext) : DbContext(options), ITenantDbContext
{
    public const string SchemaName = "test_second_schema";

    public DbSet<TenantRecord> TenantRecords => Set<TenantRecord>();

    public ITenantContext TenantContext { get; } = tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.Entity<TenantRecord>();
        modelBuilder.Entity<TenantRecord>().HasTenantIndex(
            "ix_second_schema_tenant_records_organization_id_name",
            nameof(TenantRecord.Name));
        modelBuilder.AddTenantConventions(this);
        modelBuilder.AddOutboxConventions();
    }
}
