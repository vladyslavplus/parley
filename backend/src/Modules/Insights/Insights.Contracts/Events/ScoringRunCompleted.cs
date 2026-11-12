using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Insights.Contracts.Events;

/// <summary>
/// A scoring run finished and its scores are live. Carries identifiers and the model identity only. The starter is the
/// user who requested the run (null for a run started by the platform). Published through the outbox in the same
/// transaction that swaps the live scores.
/// </summary>
public sealed record ScoringRunCompleted : IntegrationEvent
{
    public Guid ScoringRunId { get; init; }

    public string ScoreKind { get; init; } = string.Empty;

    public string ModelVersion { get; init; } = string.Empty;

    public Guid? StartedByUserId { get; init; }
}
