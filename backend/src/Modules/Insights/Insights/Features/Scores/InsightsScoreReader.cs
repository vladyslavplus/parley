using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Rfm;
using Parley.Insights.Features.Daily;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Scores;

/// <summary>
/// The score reader the campaigns module uses. RFM scores and segments come from the day the caller resolved, which is
/// always a complete day; every other kind comes from <c>latest_scores</c>, which stays empty until a model is scored.
/// </summary>
public sealed class InsightsScoreReader(InsightsDbContext dbContext) : IScoreReader
{
    public async Task<bool> HasScoresAsync(string scoreKind, CancellationToken cancellationToken) =>
        scoreKind == ScoreKinds.Rfm
            ? await DailyRunStamps.LatestRfmDayAsync(dbContext, cancellationToken) is not null
            : await dbContext.LatestScores.AnyAsync(score => score.ScoreKind == scoreKind, cancellationToken);

    public Task<DateOnly?> ReadLatestRfmDayAsync(CancellationToken cancellationToken) =>
        DailyRunStamps.LatestRfmDayAsync(dbContext, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, ContactScore>> ReadLatestScoresAsync(
        string scoreKind,
        IReadOnlyList<Guid> contactIds,
        DateOnly? rfmDay,
        CancellationToken cancellationToken)
    {
        var ids = contactIds.ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, ContactScore>();
        }

        if (scoreKind == ScoreKinds.Rfm)
        {
            return await ReadRfmScoresAsync(ids, rfmDay, cancellationToken);
        }

        var scores = await dbContext.LatestScores.AsNoTracking()
            .Where(score => score.ScoreKind == scoreKind && ids.Contains(score.ContactId))
            .Select(score => new { score.ContactId, score.Value, score.ModelVersion })
            .ToListAsync(cancellationToken);
        return scores.ToDictionary(score => score.ContactId, score => new ContactScore(score.Value, score.ModelVersion));
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ReadRfmSegmentsAsync(
        IReadOnlyList<Guid> contactIds,
        DateOnly? rfmDay,
        CancellationToken cancellationToken)
    {
        var ids = contactIds.ToArray();
        if (ids.Length == 0 || rfmDay is not { } day)
        {
            return new Dictionary<Guid, string>();
        }

        var segments = await dbContext.RfmSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.AsOf == day && ids.Contains(snapshot.ContactId))
            .Select(snapshot => new { snapshot.ContactId, snapshot.Segment })
            .ToListAsync(cancellationToken);
        return segments.ToDictionary(row => row.ContactId, row => row.Segment);
    }

    private async Task<IReadOnlyDictionary<Guid, ContactScore>> ReadRfmScoresAsync(Guid[] ids, DateOnly? rfmDay, CancellationToken cancellationToken)
    {
        if (rfmDay is not { } day)
        {
            return new Dictionary<Guid, ContactScore>();
        }

        var tiers = await dbContext.RfmSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.AsOf == day && ids.Contains(snapshot.ContactId) && snapshot.RTier != null)
            .Select(snapshot => new { snapshot.ContactId, snapshot.RTier, snapshot.FTier, snapshot.MTier })
            .ToListAsync(cancellationToken);
        return tiers.ToDictionary(
            row => row.ContactId,
            row => new ContactScore(
                RfmScoring.ScoreValue(new RfmTiers(row.RTier!.Value, row.FTier!.Value, row.MTier!.Value)),
                RfmScoring.ModelVersion));
    }
}
