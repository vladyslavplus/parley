using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.SharedKernel.Persistence.Entities;

namespace Parley.SharedKernel.Persistence.Conventions;

/// <summary>
/// Composite index helper. <c>organization_id</c> is always the first column;
/// indexes without it are rejected by requiring the helper.
/// </summary>
public static class TenantIndexExtensions
{
    public static IndexBuilder HasTenantIndex<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string indexName,
        params string[] propertyNames)
        where TEntity : BusinessEntity
    {
        var columns = BuildColumns(propertyNames);
        return builder.HasIndex(columns.ToArray()).HasDatabaseName(indexName);
    }

    public static IndexBuilder HasTenantUniqueIndex<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        string indexName,
        params string[] propertyNames)
        where TEntity : BusinessEntity
    {
        var columns = BuildColumns(propertyNames);
        return builder.HasIndex(columns.ToArray()).HasDatabaseName(indexName).IsUnique();
    }

    private static IReadOnlyList<string> BuildColumns(string[] propertyNames)
    {
        if (propertyNames is null)
        {
            throw new ArgumentNullException(nameof(propertyNames));
        }

        var columns = new List<string> { nameof(BusinessEntity.OrganizationId) };
        foreach (var propertyName in propertyNames)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentException("Index property names must not be empty.", nameof(propertyNames));
            }

            if (!string.Equals(propertyName, nameof(BusinessEntity.OrganizationId), StringComparison.Ordinal))
            {
                columns.Add(propertyName);
            }
        }

        return columns;
    }
}
