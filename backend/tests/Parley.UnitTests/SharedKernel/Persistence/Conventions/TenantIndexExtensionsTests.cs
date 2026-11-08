using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Persistence.Entities;
using Xunit;

namespace Parley.UnitTests.SharedKernel.Persistence.Conventions;

public sealed class TenantIndexExtensionsTests
{
    private const string UniqueIndexName = "ux_tenant_index_samples_organization_id_name";

    private const string PlainIndexName = "ix_tenant_index_samples_organization_id_name";

    [Fact]
    public void HasTenantUniqueIndex_UsesTheGivenIndexNameAndLeadsWithOrganization()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TenantIndexSample>(entity =>
            entity.HasTenantUniqueIndex(UniqueIndexName, nameof(TenantIndexSample.Name)));

        var index = modelBuilder.FinalizeModel()
            .FindEntityType(typeof(TenantIndexSample))!
            .GetIndexes()
            .Single();

        index.GetDatabaseName().Should().Be(UniqueIndexName);
        index.IsUnique.Should().BeTrue();
        index.Properties.Select(property => property.Name).Should().Equal(
            nameof(BusinessEntity.OrganizationId),
            nameof(TenantIndexSample.Name));
    }

    [Fact]
    public void HasTenantIndex_UsesTheGivenIndexNameAndIsNotUnique()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TenantIndexSample>(entity =>
            entity.HasTenantIndex(PlainIndexName, nameof(TenantIndexSample.Name)));

        var index = modelBuilder.FinalizeModel()
            .FindEntityType(typeof(TenantIndexSample))!
            .GetIndexes()
            .Single();

        index.GetDatabaseName().Should().Be(PlainIndexName);
        index.IsUnique.Should().BeFalse();
    }

    private sealed class TenantIndexSample : BusinessEntity
    {
        public string Name { get; set; } = string.Empty;
    }
}
