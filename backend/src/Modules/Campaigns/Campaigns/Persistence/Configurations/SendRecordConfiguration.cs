using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Campaigns.Persistence.Configurations;

/// <summary>
/// Mapping of <see cref="SendRecord"/>. The unique key on campaign and contact is the guard against a second send: the
/// claim insert ignores a contact that already has a row, so a recipient is claimed at most once.
/// </summary>
public sealed class SendRecordConfiguration : IEntityTypeConfiguration<SendRecord>
{
    private static readonly string StatusCheckSql =
        "status IN (" + string.Join(", ", SendStatuses.All.Select(status => $"'{status}'")) + ")";

    public void Configure(EntityTypeBuilder<SendRecord> builder)
    {
        builder.ToTable(CampaignsSchemaNames.SendRecordsTable, table => table.HasCheckConstraint(
            CampaignsSchemaNames.SendStatusCheck,
            StatusCheckSql));
        builder.Property(record => record.Status).HasMaxLength(CampaignsLimits.StatusMaximumLength).IsRequired();
        builder.Property(record => record.ClaimedAt).HasColumnType("timestamptz");
        builder.Property(record => record.LeaseExpiresAt).HasColumnType("timestamptz").HasConversion(CampaignsUtcValueConverters.Optional);
        builder.Property(record => record.SubmittedAt).HasColumnType("timestamptz").HasConversion(CampaignsUtcValueConverters.Optional);
        builder.Property(record => record.SentAt).HasColumnType("timestamptz").HasConversion(CampaignsUtcValueConverters.Optional);
        builder.Property(record => record.MessageId).HasMaxLength(CampaignsLimits.MessageIdMaximumLength).IsRequired();
        builder.Property(record => record.ErrorCode).HasMaxLength(CampaignsLimits.ErrorCodeMaximumLength);
        builder.HasIndex(nameof(SendRecord.CampaignId), nameof(SendRecord.ContactId))
            .IsUnique()
            .HasDatabaseName(CampaignsSchemaNames.SendCampaignContactIndex);
        builder.HasTenantIndex(
            CampaignsSchemaNames.SendClaimIndex,
            nameof(SendRecord.CampaignId),
            nameof(SendRecord.Status));
    }
}
