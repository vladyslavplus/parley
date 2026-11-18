using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Features.Daily;
using Parley.Insights.Persistence;
using Parley.Insights.Shared;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Priority.Segments;

/// <summary>
/// Counts both segment taxonomies for the latest priority day. The on-list numbers come from the day's rows; the totals come
/// from the latest RFM snapshot and from the latest uplift scores, each capped by the caller's scope. The uplift scores carry
/// no owner, so their contacts are scoped through the snapshot of the latest RFM day, which holds every contact of the
/// organization including the ones without orders.
/// </summary>
public sealed class GetPrioritySegmentCountsQueryHandler(InsightsDbContext dbContext, IDataScope dataScope)
    : IRequestHandler<GetPrioritySegmentCountsQuery, Result<PrioritySegmentCountsView>>
{
    public async ValueTask<Result<PrioritySegmentCountsView>> Handle(
        GetPrioritySegmentCountsQuery query,
        CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        var priorityDay = await DailyRunStamps.LatestPriorityDayAsync(dbContext, cancellationToken);
        var rfmDay = await DailyRunStamps.LatestRfmDayAsync(dbContext, cancellationToken);

        var rfmOnList = new Dictionary<string, int>(StringComparer.Ordinal);
        var upliftOnList = new Dictionary<string, int>(StringComparer.Ordinal);
        if (priorityDay is { } day)
        {
            var items = InsightsScopeFilter.Priority(
                dbContext.PriorityItems.AsNoTracking().Where(item => item.AsOf == day),
                scope);
            var rfmRows = await items
                .GroupBy(item => item.Segment)
                .Select(group => new { Segment = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);
            foreach (var row in rfmRows)
            {
                rfmOnList[row.Segment] = row.Count;
            }

            var upliftRows = await items
                .Where(item => item.UpliftSegment != null)
                .GroupBy(item => item.UpliftSegment!)
                .Select(group => new { Segment = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);
            foreach (var row in upliftRows)
            {
                upliftOnList[row.Segment] = row.Count;
            }
        }

        var rfmTotals = new Dictionary<string, int>(StringComparer.Ordinal);
        var upliftTotals = new Dictionary<string, int>(StringComparer.Ordinal);
        DateOnly? upliftAsOf = null;
        if (rfmDay is { } snapshotDay)
        {
            var rfmRows = await InsightsScopeFilter.Snapshots(
                    dbContext.RfmSnapshots.AsNoTracking().Where(snapshot => snapshot.AsOf == snapshotDay),
                    scope)
                .GroupBy(snapshot => snapshot.Segment)
                .Select(group => new { Segment = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);
            foreach (var row in rfmRows)
            {
                rfmTotals[row.Segment] = row.Count;
            }

            // The scores of the read path hold no owner: the snapshot of the latest RFM day tells which contacts are in scope.
            var visibleContacts = InsightsScopeFilter.Snapshots(
                    dbContext.RfmSnapshots.AsNoTracking().Where(snapshot => snapshot.AsOf == snapshotDay),
                    scope)
                .Select(snapshot => snapshot.ContactId);
            var scores = await dbContext.LatestScores.AsNoTracking()
                .Where(score => score.ScoreKind == ScoreKinds.Uplift && visibleContacts.Contains(score.ContactId))
                .Select(score => new { score.Payload, score.ScoredAt })
                .ToListAsync(cancellationToken);
            var latestScoreAt = DateTime.MinValue;
            foreach (var score in scores)
            {
                if (ReadSegment(score.Payload) is not { } segment)
                {
                    continue;
                }

                upliftTotals[segment] = upliftTotals.GetValueOrDefault(segment) + 1;
                if (score.ScoredAt > latestScoreAt)
                {
                    latestScoreAt = score.ScoredAt;
                }
            }

            upliftAsOf = scores.Count == 0 ? null : DateOnly.FromDateTime(latestScoreAt);
        }

        var rfm = RfmSegmentNames.All
            .Select(segment => new PrioritySegmentCountView(
                segment,
                rfmOnList.GetValueOrDefault(segment),
                rfmTotals.GetValueOrDefault(segment)))
            .ToList();
        var uplift = PriorityUpliftSegments.All
            .Select(segment => new PrioritySegmentCountView(
                segment,
                upliftOnList.GetValueOrDefault(segment),
                upliftTotals.GetValueOrDefault(segment)))
            .ToList();
        return Result<PrioritySegmentCountsView>.Success(
            new PrioritySegmentCountsView(priorityDay, rfmDay, upliftAsOf, rfm, uplift));
    }

    /// <summary>The segment label the uplift model stored in the score payload, or null when the payload has none.</summary>
    private static string? ReadSegment(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.TryGetProperty(ScorePayloadKeys.Segment, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
