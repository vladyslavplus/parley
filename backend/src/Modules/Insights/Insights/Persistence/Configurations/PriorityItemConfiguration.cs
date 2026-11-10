using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Shared;

namespace Parley.Insights.Persistence.Configurations;

/// <summary>Mapping of <see cref="PriorityItem"/>. The unique key is the contact of one organization day.</summary>
public sealed class PriorityItemConfiguration : IEntityTypeConfiguration<PriorityItem>
{
    public void Configure(EntityTypeBuilder<PriorityItem> builder)
    {
        builder.ToTable(InsightsSchemaNames.PriorityItemsTable);
        builder.Property(item => item.Mode).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(item => item.Segment).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(item => item.SuggestedAction).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(item => item.PriorityValue).HasColumnType("numeric(18,6)");
        builder.HasIndex(nameof(PriorityItem.OrganizationId), nameof(PriorityItem.AsOf), nameof(PriorityItem.ContactId))
            .IsUnique()
            .HasDatabaseName(InsightsSchemaNames.PriorityItemContactDayIndex);
        builder.HasIndex(nameof(PriorityItem.OrganizationId), nameof(PriorityItem.AsOf), nameof(PriorityItem.OwnerUserId))
            .HasDatabaseName(InsightsSchemaNames.PriorityItemOwnerDayIndex);
    }
}
