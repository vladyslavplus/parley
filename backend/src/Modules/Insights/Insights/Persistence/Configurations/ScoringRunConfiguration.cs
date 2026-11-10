using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Shared;

namespace Parley.Insights.Persistence.Configurations;

/// <summary>Mapping of <see cref="ScoringRun"/>.</summary>
public sealed class ScoringRunConfiguration : IEntityTypeConfiguration<ScoringRun>
{
    public void Configure(EntityTypeBuilder<ScoringRun> builder)
    {
        builder.ToTable(InsightsSchemaNames.ScoringRunsTable);
        builder.Property(run => run.Kind).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(run => run.Status).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(run => run.ErrorCode).HasMaxLength(InsightsLimits.CodeMaximumLength);
        builder.Property(run => run.ModelName).HasMaxLength(InsightsLimits.VersionMaximumLength).IsRequired();
        builder.Property(run => run.ModelVersion).HasMaxLength(InsightsLimits.VersionMaximumLength).IsRequired();
        builder.Property(run => run.StartedAt).HasColumnType("timestamptz");
        builder.Property(run => run.FinishedAt).HasColumnType("timestamptz");
    }
}
