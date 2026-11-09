using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Campaigns.Domain.Suppressions;
using Parley.Campaigns.Shared;

namespace Parley.Campaigns.Persistence.Configurations;

/// <summary>Mapping of <see cref="Suppression"/>. One suppression per organization and normalized address.</summary>
public sealed class SuppressionConfiguration : IEntityTypeConfiguration<Suppression>
{
    public void Configure(EntityTypeBuilder<Suppression> builder)
    {
        builder.ToTable(CampaignsSchemaNames.SuppressionsTable);
        builder.Property(suppression => suppression.EmailNormalized).HasMaxLength(CampaignsLimits.EmailMaximumLength).IsRequired();
        builder.Property(suppression => suppression.Reason).HasMaxLength(CampaignsLimits.StatusMaximumLength).IsRequired();
        builder.Property(suppression => suppression.SuppressedAt).HasColumnType("timestamptz");
        builder.HasIndex(nameof(Suppression.OrganizationId), nameof(Suppression.EmailNormalized))
            .IsUnique()
            .HasDatabaseName(CampaignsSchemaNames.SuppressionEmailIndex);
    }
}
