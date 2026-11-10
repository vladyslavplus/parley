using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Campaigns.Domain.Attribution;

namespace Parley.Campaigns.Persistence.Configurations;

/// <summary>Mapping of <see cref="CampaignResult"/>: one row per campaign, rebuilt by the attribution job.</summary>
public sealed class CampaignResultConfiguration : IEntityTypeConfiguration<CampaignResult>
{
    public void Configure(EntityTypeBuilder<CampaignResult> builder)
    {
        builder.ToTable(CampaignsSchemaNames.CampaignResultsTable);
        builder.Property(result => result.TreatmentRevenue).HasColumnType("numeric(18,2)");
        builder.Property(result => result.HoldoutRevenue).HasColumnType("numeric(18,2)");
        builder.Property(result => result.WindowEndsAt).HasColumnType("timestamptz");
        builder.Property(result => result.ComputedAt).HasColumnType("timestamptz");
        builder.HasIndex(nameof(CampaignResult.CampaignId))
            .IsUnique()
            .HasDatabaseName(CampaignsSchemaNames.ResultCampaignIndex);
    }
}
