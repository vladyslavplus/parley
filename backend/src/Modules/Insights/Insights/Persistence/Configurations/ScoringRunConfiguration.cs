using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Shared;

namespace Parley.Insights.Persistence.Configurations;

/// <summary>
/// Mapping of <see cref="ScoringRun"/>. One run of a kind may be active per organization: the partial unique index is the
/// guard a racing start hits. The completed lookup reads the latest completed run of a kind.
/// </summary>
public sealed class ScoringRunConfiguration : IEntityTypeConfiguration<ScoringRun>
{
    public void Configure(EntityTypeBuilder<ScoringRun> builder)
    {
        builder.ToTable(InsightsSchemaNames.ScoringRunsTable);
        builder.Property(run => run.Kind).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(run => run.Status).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(run => run.ErrorCode).HasMaxLength(InsightsLimits.ErrorCodeMaximumLength);
        builder.Property(run => run.ModelName).HasMaxLength(InsightsLimits.VersionMaximumLength).IsRequired();
        builder.Property(run => run.ModelVersion).HasMaxLength(InsightsLimits.VersionMaximumLength).IsRequired();
        builder.Property(run => run.StartedAt).HasColumnType("timestamptz");
        builder.Property(run => run.FinishedAt).HasColumnType("timestamptz");
        builder.HasIndex(run => new { run.OrganizationId, run.Kind })
            .IsUnique()
            .HasFilter($"status IN ('{ScoringRunStatuses.Queued}', '{ScoringRunStatuses.Running}')")
            .HasDatabaseName(InsightsSchemaNames.ScoringRunActiveIndex);
        builder.HasIndex(run => new { run.OrganizationId, run.Kind, run.Status, run.FinishedAt })
            .HasDatabaseName(InsightsSchemaNames.ScoringRunLatestIndex);
    }
}
