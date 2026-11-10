using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Insights.Domain.Rfm;
using Parley.Insights.Shared;

namespace Parley.Insights.Persistence.Configurations;

/// <summary>Mapping of <see cref="RfmSnapshot"/>. The unique key makes the daily job idempotent per contact and day.</summary>
public sealed class RfmSnapshotConfiguration : IEntityTypeConfiguration<RfmSnapshot>
{
    public void Configure(EntityTypeBuilder<RfmSnapshot> builder)
    {
        builder.ToTable(InsightsSchemaNames.RfmSnapshotsTable);
        builder.Property(snapshot => snapshot.Monetary).HasColumnType("numeric(18,2)");
        builder.Property(snapshot => snapshot.RTier).HasColumnType("smallint");
        builder.Property(snapshot => snapshot.FTier).HasColumnType("smallint");
        builder.Property(snapshot => snapshot.MTier).HasColumnType("smallint");
        builder.Property(snapshot => snapshot.Segment).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.HasIndex(nameof(RfmSnapshot.OrganizationId), nameof(RfmSnapshot.ContactId), nameof(RfmSnapshot.AsOf))
            .IsUnique()
            .HasDatabaseName(InsightsSchemaNames.RfmSnapshotContactDayIndex);
        builder.HasIndex(nameof(RfmSnapshot.OrganizationId), nameof(RfmSnapshot.AsOf), nameof(RfmSnapshot.Segment))
            .HasDatabaseName(InsightsSchemaNames.RfmSnapshotDaySegmentIndex);
    }
}
