using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Crm.Domain.Tags;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Mapping of <see cref="Tag"/>: names unique per organization ignoring case.</summary>
public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable(CrmSchemaNames.TagsTable, table => table.HasCheckConstraint(
            CrmSchemaNames.ColorCheckConstraint,
            CrmSql.TagColorCheck));
        builder.Property(tag => tag.Name).HasMaxLength(CrmLimits.TagNameMaximumLength).IsRequired();
        builder.Property(tag => tag.ColorIndex).HasColumnType("smallint");
        builder.Property(tag => tag.NameNormalized)
            .HasMaxLength(CrmLimits.TagNameMaximumLength)
            .HasComputedColumnSql(CrmSql.TagNameNormalized, stored: true);
        builder.HasTenantUniqueIndex(CrmSchemaNames.TagNameIndex, nameof(Tag.NameNormalized));
        builder.HasTenantIndex(CrmSchemaNames.TagIdIndex, nameof(Tag.Id));
    }
}
