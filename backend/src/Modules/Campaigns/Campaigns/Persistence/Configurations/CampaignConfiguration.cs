using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Campaigns.Persistence.Configurations;

/// <summary>Mapping of <see cref="Campaign"/>.</summary>
public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable(CampaignsSchemaNames.CampaignsTable);
        builder.Property(campaign => campaign.Name).HasMaxLength(CampaignsLimits.NameMaximumLength).IsRequired();
        builder.Property(campaign => campaign.Status).HasMaxLength(CampaignsLimits.StatusMaximumLength).IsRequired();
        builder.Property(campaign => campaign.HoldoutSeed).HasMaxLength(CampaignsLimits.HoldoutSeedMaximumLength).IsRequired();
        builder.Property(campaign => campaign.TargetingMode).HasMaxLength(CampaignsLimits.StatusMaximumLength).IsRequired();
        builder.Property(campaign => campaign.ConversionRule).HasMaxLength(CampaignsLimits.StatusMaximumLength).IsRequired();
        builder.Property(campaign => campaign.Source).HasMaxLength(CampaignsLimits.StatusMaximumLength).IsRequired();
        foreach (var instant in new[] { nameof(Campaign.PrepareAsOf), nameof(Campaign.AudienceFrozenAt), nameof(Campaign.StartedAt), nameof(Campaign.CompletedAt) })
        {
            builder.Property(instant).HasColumnType("timestamptz").HasConversion(CampaignsUtcValueConverters.Optional);
        }

        builder.HasTenantIndex(CampaignsSchemaNames.CampaignListIndex, nameof(Campaign.Name), nameof(Campaign.Id));
    }
}
