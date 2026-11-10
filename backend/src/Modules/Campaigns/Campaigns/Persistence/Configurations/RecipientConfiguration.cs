using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Campaigns.Persistence.Configurations;

/// <summary>
/// Mapping of <see cref="Recipient"/>. The unique key on campaign and contact is the guard the freeze relies on: its
/// insert ignores a row that already exists, so a resumed freeze keeps the first snapshot of every contact.
/// </summary>
public sealed class RecipientConfiguration : IEntityTypeConfiguration<Recipient>
{
    public void Configure(EntityTypeBuilder<Recipient> builder)
    {
        builder.ToTable(CampaignsSchemaNames.RecipientsTable);
        builder.Property(recipient => recipient.Arm).HasMaxLength(CampaignsLimits.StatusMaximumLength).IsRequired();
        builder.Property(recipient => recipient.ScoreKind).HasMaxLength(CampaignsLimits.StatusMaximumLength);
        builder.Property(recipient => recipient.ScoreValue).HasPrecision(18, 6);
        builder.Property(recipient => recipient.ModelVersion).HasMaxLength(CampaignsLimits.NameMaximumLength);
        builder.Property(recipient => recipient.EmailSnapshot).HasMaxLength(CampaignsLimits.EmailMaximumLength);
        builder.Property(recipient => recipient.Locale).HasMaxLength(CampaignsLimits.LocaleMaximumLength).IsRequired();
        builder.HasIndex(nameof(Recipient.CampaignId), nameof(Recipient.ContactId))
            .IsUnique()
            .HasDatabaseName(CampaignsSchemaNames.RecipientCampaignContactIndex);
        builder.HasTenantIndex(
            CampaignsSchemaNames.RecipientArmIndex,
            nameof(Recipient.CampaignId),
            nameof(Recipient.Arm),
            nameof(Recipient.IsTargeted));
    }
}
