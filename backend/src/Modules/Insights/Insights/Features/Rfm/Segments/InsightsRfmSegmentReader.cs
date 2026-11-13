using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Features.Daily;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Rfm.Segments;

/// <summary>
/// The segment reader the contacts module uses. Reads the latest complete snapshot day of the tenant only.
/// </summary>
public sealed class InsightsRfmSegmentReader(InsightsDbContext dbContext) : IRfmSegmentReader
{
    public async Task<IReadOnlyDictionary<Guid, string>> ReadSegmentsAsync(
        IReadOnlyCollection<Guid> contactIds,
        CancellationToken cancellationToken)
    {
        var ids = contactIds.ToList();
        var day = await DailyRunStamps.LatestRfmDayAsync(dbContext, cancellationToken);
        if (ids.Count == 0 || day is null)
        {
            return new Dictionary<Guid, string>();
        }

        var rows = await dbContext.RfmSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.AsOf == day.Value && ids.Contains(snapshot.ContactId))
            .Select(snapshot => new { snapshot.ContactId, snapshot.Segment })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(row => row.ContactId, row => row.Segment);
    }

    public async Task<RfmSegmentMembers> ReadMemberIdsAsync(
        string segment,
        int maximum,
        IReadOnlyCollection<Guid>? visibleOwnerUserIds,
        CancellationToken cancellationToken)
    {
        var day = await DailyRunStamps.LatestRfmDayAsync(dbContext, cancellationToken);
        if (day is null)
        {
            return new RfmSegmentMembers([], Truncated: false);
        }

        var members = dbContext.RfmSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.AsOf == day.Value && snapshot.Segment == segment);
        if (visibleOwnerUserIds is not null)
        {
            var owners = visibleOwnerUserIds.ToList();
            members = members.Where(snapshot => snapshot.OwnerUserId != null && owners.Contains(snapshot.OwnerUserId.Value));
        }

        // One row past the maximum tells the caller whether the list is complete.
        var ids = await members
            .OrderBy(snapshot => snapshot.ContactId)
            .Select(snapshot => snapshot.ContactId)
            .Take(maximum + 1)
            .ToListAsync(cancellationToken);
        return new RfmSegmentMembers(ids.Take(maximum).ToArray(), ids.Count > maximum);
    }
}
