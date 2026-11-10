using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Shared;

namespace Parley.Insights.Persistence.Configurations;

/// <summary>Mapping of <see cref="LatestScore"/>: keyed by organization, contact and kind; swapped only when a run completes.</summary>
/// <remarks>The tenant filter is declared on the context, next to the other tenant rules.</remarks>
public sealed class LatestScoreConfiguration : IEntityTypeConfiguration<LatestScore>
{
    public void Configure(EntityTypeBuilder<LatestScore> builder)
    {
        builder.ToTable(InsightsSchemaNames.LatestScoresTable);
        builder.HasKey(nameof(LatestScore.OrganizationId), nameof(LatestScore.ContactId), nameof(LatestScore.ScoreKind));
        builder.Property(score => score.ScoreKind).HasMaxLength(InsightsLimits.CodeMaximumLength).IsRequired();
        builder.Property(score => score.ModelVersion).HasMaxLength(InsightsLimits.VersionMaximumLength).IsRequired();
        builder.Property(score => score.Value).HasColumnType("numeric(18,6)");
        builder.Property(score => score.Payload).HasColumnType("jsonb");
        builder.Property(score => score.ScoredAt).HasColumnType("timestamptz");
    }
}
