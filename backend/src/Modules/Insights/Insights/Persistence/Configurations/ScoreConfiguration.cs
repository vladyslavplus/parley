using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Shared;

namespace Parley.Insights.Persistence.Configurations;

/// <summary>Mapping of <see cref="Score"/>: the history, unique per run, contact and kind.</summary>
public sealed class ScoreConfiguration : IEntityTypeConfiguration<Score>
{
    public void Configure(EntityTypeBuilder<Score> builder)
    {
        builder.ToTable(InsightsSchemaNames.ScoresTable);
        builder.Property(score => score.ScoreKind).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(score => score.ModelName).HasMaxLength(InsightsLimits.VersionMaximumLength).IsRequired();
        builder.Property(score => score.ModelVersion).HasMaxLength(InsightsLimits.VersionMaximumLength).IsRequired();
        builder.Property(score => score.Value).HasColumnType("numeric(18,6)");
        builder.Property(score => score.Payload).HasColumnType("jsonb");
        builder.Property(score => score.ScoredAt).HasColumnType("timestamptz");
        builder.HasIndex(nameof(Score.ScoringRunId), nameof(Score.ContactId), nameof(Score.ScoreKind))
            .IsUnique()
            .HasDatabaseName(InsightsSchemaNames.ScoreRunContactKindIndex);
    }
}
