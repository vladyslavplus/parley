using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Parley.SharedKernel.Persistence.Entities;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.SharedKernel.Persistence.Conventions;

/// <summary>
/// EF conventions applied once per module DbContext in <c>OnModelCreating</c>.
/// The query filter is rooted at the executing <c>DbContext</c> so EF re-reads the
/// executing context's tenant on every query; a tenant captured as a value at
/// model-build time would be baked into the compiled plan for later calls.
/// An unset tenant throws <see cref="TenantContextNotSetException"/> instead
/// of returning all rows.
/// </summary>
public static class TenantModelBuilderExtensions
{
    public static ModelBuilder AddTenantConventions(this ModelBuilder modelBuilder, DbContext context)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(context);

        if (context is not ITenantDbContext)
        {
            throw new ArgumentException("DbContexts applying tenant conventions must implement ITenantDbContext.", nameof(context));
        }

        var baseBuilder = modelBuilder.Entity<BusinessEntity>();
        baseBuilder.UseTpcMappingStrategy();
        baseBuilder.Property(entity => entity.OrganizationId).IsRequired();
        baseBuilder.HasQueryFilter(entity => entity.OrganizationId == ((ITenantDbContext)context).TenantContext.OrganizationId);

        baseBuilder.Property(entity => entity.CreatedAt)
            .HasColumnType("timestamptz")
            .HasConversion(
                new ValueConverter<DateTime, DateTime>(
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        baseBuilder.Property(entity => entity.UpdatedAt)
            .HasColumnType("timestamptz")
            .HasConversion(
                new ValueConverter<DateTime, DateTime>(
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                    value => DateTime.SpecifyKind(value, DateTimeKind.Utc)));

        baseBuilder.Property<uint>(PersistenceConstants.XminPropertyName)
            .IsRowVersion()
            .HasColumnType("xid");

        return modelBuilder;
    }

    public static DbContextOptionsBuilder AddTenantConventions(this DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        return optionsBuilder.UseSnakeCaseNamingConvention();
    }
}
