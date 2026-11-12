using Parley.Insights.Domain.Ml;

namespace Parley.Insights.Features.Scoring.Shared;

/// <summary>One scoring run as the API shows it: its kind, the pinned model, its state and its counts.</summary>
public sealed record ScoringRunView(
    Guid Id,
    string Kind,
    string ModelName,
    string ModelVersion,
    string Status,
    int ContactsTotal,
    int ContactsScored,
    int ContactsRejected,
    string? ErrorCode,
    DateTime StartedAt,
    DateTime? FinishedAt)
{
    public static ScoringRunView From(ScoringRun run) => new(
        run.Id,
        run.Kind,
        run.ModelName,
        run.ModelVersion,
        run.Status,
        run.ContactsTotal,
        run.ContactsScored,
        run.ContactsRejected,
        run.ErrorCode,
        run.StartedAt,
        run.FinishedAt);
}
