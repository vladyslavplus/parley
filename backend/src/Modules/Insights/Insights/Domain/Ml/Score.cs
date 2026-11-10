using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Insights.Domain.Ml;

/// <summary>One score of a contact from a scoring run. The history of the scores; the read path is <see cref="LatestScore"/>.</summary>
public sealed class Score : BusinessEntity
{
    public Guid ScoringRunId { get; set; }

    public Guid ContactId { get; set; }

    public string ScoreKind { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = string.Empty;

    public int FeatureSchemaVersion { get; set; }

    public decimal Value { get; set; }

    public string Payload { get; set; } = "{}";

    public DateTime ScoredAt { get; set; }
}
