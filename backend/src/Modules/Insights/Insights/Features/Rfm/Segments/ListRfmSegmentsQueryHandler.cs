using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Features.Daily;
using Parley.Insights.Persistence;
using Parley.Insights.Shared;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Rfm.Segments;

/// <summary>Segment counts of the latest snapshot day, in the documented segment order. Every segment is listed, even when empty.</summary>
public sealed class ListRfmSegmentsQueryHandler(InsightsDbContext dbContext, IDataScope dataScope)
    : IRequestHandler<ListRfmSegmentsQuery, Result<IReadOnlyList<RfmSegmentCountView>>>
{
    public async ValueTask<Result<IReadOnlyList<RfmSegmentCountView>>> Handle(ListRfmSegmentsQuery query, CancellationToken cancellationToken)
    {
        var day = await DailyRunStamps.LatestRfmDayAsync(dbContext, cancellationToken);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        if (day is not null)
        {
            var scope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
            var rows = await InsightsScopeFilter.Snapshots(
                    dbContext.RfmSnapshots.AsNoTracking().Where(snapshot => snapshot.AsOf == day.Value),
                    scope)
                .GroupBy(snapshot => snapshot.Segment)
                .Select(group => new { Segment = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);
            foreach (var row in rows)
            {
                counts[row.Segment] = row.Count;
            }
        }

        IReadOnlyList<RfmSegmentCountView> views = RfmSegmentNames.All
            .Select(segment => new RfmSegmentCountView(segment, counts.GetValueOrDefault(segment)))
            .ToList();
        return Result<IReadOnlyList<RfmSegmentCountView>>.Success(views);
    }
}
