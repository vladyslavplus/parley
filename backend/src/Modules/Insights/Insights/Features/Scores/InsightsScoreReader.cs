using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Rfm;
using Parley.Insights.Features.Daily;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Scores;

/// <summary>
/// The score reader the campaigns module and the priority build use. RFM scores and segments come from the day the caller
/// resolved, which is always a complete day. The ML kinds come from the completed scoring runs: <c>latest_scores</c> holds
/// the scores of the latest completed run of each kind, and a caller that pins a run reads that run's own rows, so an
/// audience frozen over several batches never mixes two runs.
/// </summary>
public sealed class InsightsScoreReader(InsightsDbContext dbContext) : IScoreReader
{
    private const string SegmentKey = "segment";

    /// <summary>ML kinds count only while ML is on and a run of the version in use has completed; RFM counts from its complete day.</summary>
    public async Task<bool> HasScoresAsync(string scoreKind, CancellationToken cancellationToken) =>
        scoreKind == ScoreKinds.Rfm
            ? await DailyRunStamps.LatestRfmDayAsync(dbContext, cancellationToken) is not null
            : await ActiveScoringRuns.FindActiveRunIdAsync(dbContext, scoreKind, cancellationToken) is not null;

    public Task<DateOnly?> ReadLatestRfmDayAsync(CancellationToken cancellationToken) =>
        DailyRunStamps.LatestRfmDayAsync(dbContext, cancellationToken);

    /// <summary>The newest completed run that scored with the version in use now. Null while ML is off.</summary>
    public Task<Guid?> ReadLatestScoringRunAsync(string scoreKind, CancellationToken cancellationToken) =>
        ActiveScoringRuns.FindActiveRunIdAsync(dbContext, scoreKind, cancellationToken);

    public async Task<ScoringRunModel?> ReadScoringRunModelAsync(Guid scoringRunId, CancellationToken cancellationToken)
    {
        var run = await dbContext.ScoringRuns.AsNoTracking()
            .Where(row => row.Id == scoringRunId)
            .Select(row => new { row.ModelName, row.ModelVersion })
            .FirstOrDefaultAsync(cancellationToken);
        if (run is null)
        {
            return null;
        }

        var provenance = await dbContext.ModelVersions.AsNoTracking()
            .Where(model => model.ModelName == run.ModelName && model.Version == run.ModelVersion)
            .Select(model => model.Provenance)
            .FirstOrDefaultAsync(cancellationToken);
        return new ScoringRunModel(run.ModelName, run.ModelVersion, provenance ?? string.Empty);
    }

    public async Task<IReadOnlyDictionary<Guid, ContactScore>> ReadLatestScoresAsync(
        string scoreKind,
        IReadOnlyList<Guid> contactIds,
        DateOnly? rfmDay,
        Guid? scoringRunId,
        CancellationToken cancellationToken)
    {
        var ids = contactIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, ContactScore>();
        }

        if (scoreKind == ScoreKinds.Rfm)
        {
            return await ReadRfmScoresAsync(ids, rfmDay, cancellationToken);
        }

        if (scoringRunId is { } runId)
        {
            var pinned = await dbContext.Scores.AsNoTracking()
                .Where(score => score.ScoringRunId == runId && score.ScoreKind == scoreKind && ids.Contains(score.ContactId))
                .Select(score => new { score.ContactId, score.Value, score.ModelVersion, score.Payload })
                .ToListAsync(cancellationToken);
            return pinned.ToDictionary(
                score => score.ContactId,
                score => new ContactScore(score.Value, score.ModelVersion, ReadSegment(score.Payload)));
        }

        var latest = await dbContext.LatestScores.AsNoTracking()
            .Where(score => score.ScoreKind == scoreKind && ids.Contains(score.ContactId))
            .Select(score => new { score.ContactId, score.Value, score.ModelVersion, score.Payload })
            .ToListAsync(cancellationToken);
        return latest.ToDictionary(
            score => score.ContactId,
            score => new ContactScore(score.Value, score.ModelVersion, ReadSegment(score.Payload)));
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ReadRfmSegmentsAsync(
        IReadOnlyList<Guid> contactIds,
        DateOnly? rfmDay,
        CancellationToken cancellationToken)
    {
        var ids = contactIds.ToList();
        if (ids.Count == 0 || rfmDay is not { } day)
        {
            return new Dictionary<Guid, string>();
        }

        var segments = await dbContext.RfmSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.AsOf == day && ids.Contains(snapshot.ContactId))
            .Select(snapshot => new { snapshot.ContactId, snapshot.Segment })
            .ToListAsync(cancellationToken);
        return segments.ToDictionary(row => row.ContactId, row => row.Segment);
    }

    private async Task<IReadOnlyDictionary<Guid, ContactScore>> ReadRfmScoresAsync(List<Guid> ids, DateOnly? rfmDay, CancellationToken cancellationToken)
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

    /// <summary>The segment label a model stored in the payload, or null when the kind has none.</summary>
    private static string? ReadSegment(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.TryGetProperty(SegmentKey, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
