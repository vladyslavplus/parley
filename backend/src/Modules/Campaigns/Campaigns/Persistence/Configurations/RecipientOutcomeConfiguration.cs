using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Campaigns.Domain.Attribution;

namespace Parley.Campaigns.Persistence.Configurations;

/// <summary>
/// Mapping of <see cref="RecipientOutcome"/>. The unique key on campaign and recipient makes the attribution upsert
/// idempotent: a re-run updates the row of each recipient and never adds a second one.
/// </summary>
public sealed class RecipientOutcomeConfiguration : IEntityTypeConfiguration<RecipientOutcome>
{
    public void Configure(EntityTypeBuilder<RecipientOutcome> builder)
    {
        builder.ToTable(CampaignsSchemaNames.RecipientOutcomesTable);
        builder.Property(outcome => outcome.Revenue).HasColumnType("numeric(18,2)");
        builder.Property(outcome => outcome.FirstConversionAt).HasColumnType("timestamptz");
        builder.Property(outcome => outcome.EvaluatedAt).HasColumnType("timestamptz");
        builder.HasIndex(nameof(RecipientOutcome.CampaignId), nameof(RecipientOutcome.RecipientId))
            .IsUnique()
            .HasDatabaseName(CampaignsSchemaNames.OutcomeCampaignRecipientIndex);
    }
}
