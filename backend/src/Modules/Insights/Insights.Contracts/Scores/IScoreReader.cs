namespace Parley.Insights.Contracts.Scores;

/// <summary>
/// Read path of the latest scores and RFM segments of the current organization, used to rank campaign audiences and to
/// filter them by segment. RFM values come from the latest <em>complete</em> RFM day only; a day still being written is
/// never visible. A caller that reads several batches resolves the day once with <see cref="ReadLatestRfmDayAsync"/> and
/// passes it to every batch, so one audience never mixes two days. Until the pipeline has produced a complete day,
/// <see cref="HasScoresAsync"/> is false and a campaign that targets by score, or filters by RFM segment, cannot be prepared.
/// </summary>
public interface IScoreReader
{
    /// <summary>True when at least one score of the kind exists for the current organization.</summary>
    Task<bool> HasScoresAsync(string scoreKind, CancellationToken cancellationToken);

    /// <summary>The latest complete RFM day of the current organization, or null before the first complete day.</summary>
    Task<DateOnly?> ReadLatestRfmDayAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The id of the latest <em>completed</em> scoring run of the kind, or null when none completed. A caller that reads
    /// several batches of an audience resolves it once and passes it to every batch, so the audience never mixes two runs.
    /// </summary>
    Task<Guid?> ReadLatestScoringRunAsync(string scoreKind, CancellationToken cancellationToken);

    /// <summary>
    /// The model a scoring run pinned: its name, its version and the provenance the registry holds for that version. Null when
    /// the run does not exist in the current organization. A campaign copies these values when its audience is frozen.
    /// </summary>
    Task<ScoringRunModel?> ReadScoringRunModelAsync(Guid scoringRunId, CancellationToken cancellationToken);

    /// <summary>
    /// The score of each listed contact that has one. For the RFM kind the scores come from <paramref name="rfmDay"/>; a null
    /// day yields none. For the ML kinds the scores come from <paramref name="scoringRunId"/> when it is set (the run the caller
    /// resolved), and from the latest scores otherwise. Contacts without a score are absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ContactScore>> ReadLatestScoresAsync(
        string scoreKind,
        IReadOnlyList<Guid> contactIds,
        DateOnly? rfmDay,
        Guid? scoringRunId,
        CancellationToken cancellationToken);

    /// <summary>
    /// The RFM segment name of each listed contact that has tiers on <paramref name="rfmDay"/>. A null day yields none.
    /// Contacts without orders are absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ReadRfmSegmentsAsync(
        IReadOnlyList<Guid> contactIds,
        DateOnly? rfmDay,
        CancellationToken cancellationToken);
}
