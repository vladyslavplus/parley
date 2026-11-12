using Microsoft.EntityFrameworkCore;
using Parley.Insights.Domain.Daily;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Daily;

/// <summary>
/// Completion stamps of the organization days, and the reads that take only completed days. A reader never looks at the
/// snapshot or priority rows directly for the latest day: a day is visible only after its step stamped it.
/// </summary>
public static class DailyRunStamps
{
    /// <summary>The latest day whose RFM snapshot is complete, or null before the first complete day.</summary>
    public static Task<DateOnly?> LatestRfmDayAsync(InsightsDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.DailyRuns.AsNoTracking()
            .Where(run => run.RfmCompletedAt != null)
            .MaxAsync(run => (DateOnly?)run.AsOf, cancellationToken);

    /// <summary>The latest day whose priority list is complete, or null before the first complete day.</summary>
    public static Task<DateOnly?> LatestPriorityDayAsync(InsightsDbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.DailyRuns.AsNoTracking()
            .Where(run => run.PriorityCompletedAt != null)
            .MaxAsync(run => (DateOnly?)run.AsOf, cancellationToken);

    /// <summary>Stamps the RFM step of a day as complete. Creates the day row when the day has none yet.</summary>
    public static async Task MarkRfmCompletedAsync(
        InsightsDbContext dbContext,
        Guid organizationId,
        DateOnly asOf,
        DateTime completedAt,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.DailyRuns.IgnoreQueryFilters()
            .Where(run => run.OrganizationId == organizationId && run.AsOf == asOf)
            .ExecuteUpdateAsync(setters => setters.SetProperty(run => run.RfmCompletedAt, completedAt), cancellationToken);
        if (updated == 0)
        {
            dbContext.DailyRuns.Add(new DailyRun { OrganizationId = organizationId, AsOf = asOf, RfmCompletedAt = completedAt });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>Stamps the priority step of a day as complete. Creates the day row when the day has none yet.</summary>
    public static async Task MarkPriorityCompletedAsync(
        InsightsDbContext dbContext,
        Guid organizationId,
        DateOnly asOf,
        DateTime completedAt,
        string mode,
        string mlStatus,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.DailyRuns.IgnoreQueryFilters()
            .Where(run => run.OrganizationId == organizationId && run.AsOf == asOf)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.PriorityCompletedAt, completedAt)
                    .SetProperty(run => run.PriorityMode, mode)
                    .SetProperty(run => run.PriorityMlStatus, mlStatus),
                cancellationToken);
        if (updated == 0)
        {
            dbContext.DailyRuns.Add(new DailyRun
            {
                OrganizationId = organizationId,
                AsOf = asOf,
                PriorityCompletedAt = completedAt,
                PriorityMode = mode,
                PriorityMlStatus = mlStatus,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
