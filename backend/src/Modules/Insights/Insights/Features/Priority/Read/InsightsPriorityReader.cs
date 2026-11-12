using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Priority;
using Parley.Insights.Features.Daily;
using Parley.Insights.Persistence;
using Parley.Insights.Shared;

namespace Parley.Insights.Features.Priority.Read;

/// <summary>
/// Top of the latest priority list, for other modules. Applies no user scope: the callers are platform code, so the
/// result never backs a user-facing list.
/// </summary>
public sealed class InsightsPriorityReader(InsightsDbContext dbContext) : IPriorityReader
{
    public async Task<IReadOnlyList<PriorityEntry>> ReadTopAsync(IReadOnlyCollection<Guid>? ownerUserIds, int limit, CancellationToken cancellationToken)
    {
        var day = await DailyRunStamps.LatestPriorityDayAsync(dbContext, cancellationToken);
        if (day is null)
        {
            return [];
        }

        var items = dbContext.PriorityItems.AsNoTracking().Where(item => item.AsOf == day.Value);
        if (ownerUserIds is not null)
        {
            var owners = ownerUserIds.ToArray();
            items = items.Where(item => item.OwnerUserId != null && owners.Contains(item.OwnerUserId.Value));
        }

        var rows = await items
            .OrderByDescending(item => item.PriorityValue)
            .ThenBy(item => item.ContactId)
            .Take(Math.Clamp(limit, 1, InsightsLimits.MaximumTopLimit))
            .ToListAsync(cancellationToken);
        return rows
            .Select(row => new PriorityEntry(
                row.ContactId,
                row.OwnerUserId,
                row.Rank,
                row.PriorityValue,
                row.Mode,
                row.Segment,
                row.ReasonCodes,
                row.SuggestedAction))
            .ToList();
    }
}
