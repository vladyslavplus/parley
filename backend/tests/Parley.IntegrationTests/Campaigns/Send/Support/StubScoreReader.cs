using Parley.Insights.Contracts.Scores;

namespace Parley.IntegrationTests.Campaigns.Send.Support;

/// <summary>
/// Score reader that scores every contact, so a ranking campaign has a targeted and a non-targeted group. The score is
/// derived from the contact id, which keeps the ranking deterministic.
/// </summary>
public sealed class StubScoreReader : IScoreReader
{
    private const string ModelVersion = "stub-v";

    private static readonly Guid StubRunId = Guid.Parse("0192f5a1-0000-7000-8000-0000000000aa");

    public Task<bool> HasScoresAsync(string scoreKind, CancellationToken cancellationToken) => Task.FromResult(true);

    /// <summary>No RFM day: the stub does not model snapshots, so the RFM filter sees no complete day.</summary>
    public Task<DateOnly?> ReadLatestRfmDayAsync(CancellationToken cancellationToken) => Task.FromResult<DateOnly?>(null);

    /// <summary>One fixed completed run for every kind: the stub has no runs, and the freeze reads the run it is given.</summary>
    public Task<Guid?> ReadLatestScoringRunAsync(string scoreKind, CancellationToken cancellationToken) =>
        Task.FromResult<Guid?>(StubRunId);

    /// <summary>The fixed run reports the stub model, so a frozen campaign pins a model name, version and provenance.</summary>
    public Task<ScoringRunModel?> ReadScoringRunModelAsync(Guid scoringRunId, CancellationToken cancellationToken) =>
        Task.FromResult<ScoringRunModel?>(new ScoringRunModel("crm_uplift", ModelVersion + ScoreKinds.Uplift, "stub provenance"));

    public Task<IReadOnlyDictionary<Guid, ContactScore>> ReadLatestScoresAsync(
        string scoreKind,
        IReadOnlyList<Guid> contactIds,
        DateOnly? rfmDay,
        Guid? scoringRunId,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, ContactScore> scores = contactIds.ToDictionary(
            contactId => contactId,
            contactId => new ContactScore(contactId.ToByteArray()[0], ModelVersion + scoreKind));
        return Task.FromResult(scores);
    }

    /// <summary>No RFM segments: the stub does not model tiers, and the tests that use it do not filter by segment.</summary>
    public Task<IReadOnlyDictionary<Guid, string>> ReadRfmSegmentsAsync(
        IReadOnlyList<Guid> contactIds,
        DateOnly? rfmDay,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());
}
