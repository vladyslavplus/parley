using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Insights.Domain.Daily;

namespace Parley.Insights.Persistence.Configurations;

/// <summary>Mapping of <see cref="DailyRun"/>. The unique key allows one run row per organization day.</summary>
public sealed class DailyRunConfiguration : IEntityTypeConfiguration<DailyRun>
{
    public void Configure(EntityTypeBuilder<DailyRun> builder)
    {
        builder.ToTable(InsightsSchemaNames.DailyRunsTable);
        builder.Property(run => run.RfmCompletedAt).HasColumnType("timestamptz");
        builder.Property(run => run.PriorityCompletedAt).HasColumnType("timestamptz");
        builder.HasIndex(nameof(DailyRun.OrganizationId), nameof(DailyRun.AsOf))
            .IsUnique()
            .HasDatabaseName(InsightsSchemaNames.DailyRunDayIndex);
    }
}
