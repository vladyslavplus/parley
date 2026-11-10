using Parley.Insights.Contracts.Scores;

namespace Parley.IntegrationTests.Campaigns.Send.Support;

/// <summary>
/// Score reader that scores every contact, so a ranking campaign has a targeted and a non-targeted group. The score is
/// derived from the contact id, which keeps the ranking deterministic.
/// </summary>
public sealed class StubScoreReader : IScoreReader
{
    private const string ModelVersion = "stub-v";

    public Task<bool> HasScoresAsync(string scoreKind, CancellationToken cancellationToken) => Task.FromResult(true);

    /// <summary>No RFM day: the stub does not model snapshots, so the RFM filter sees no complete day.</summary>
    public Task<DateOnly?> ReadLatestRfmDayAsync(CancellationToken cancellationToken) => Task.FromResult<DateOnly?>(null);

    public Task<IReadOnlyDictionary<Guid, ContactScore>> ReadLatestScoresAsync(
        string scoreKind,
        IReadOnlyList<Guid> contactIds,
        DateOnly? rfmDay,
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
