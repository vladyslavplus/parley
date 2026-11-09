using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Campaigns.Persistence.Configurations;

/// <summary>Mapping of <see cref="Segment"/>. The definition is one jsonb column.</summary>
public sealed class SegmentConfiguration : IEntityTypeConfiguration<Segment>
{
    public void Configure(EntityTypeBuilder<Segment> builder)
    {
        builder.ToTable(CampaignsSchemaNames.SegmentsTable);
        builder.Property(segment => segment.Name).HasMaxLength(CampaignsLimits.SegmentNameMaximumLength).IsRequired();
        builder.Property(segment => segment.ArchivedAt)
            .HasColumnType("timestamptz")
            .HasConversion(CampaignsUtcValueConverters.Optional);
        builder.Property(segment => segment.Definition)
            .HasColumnName(CampaignsSchemaNames.SegmentDefinitionColumn)
            .HasColumnType("jsonb")
            .HasConversion(SegmentDefinitionJsonConverter.Instance)
            .IsRequired();
        builder.HasTenantIndex(CampaignsSchemaNames.SegmentListIndex, nameof(Segment.Name), nameof(Segment.Id));
    }
}
