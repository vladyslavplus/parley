using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Organizations;
using Parley.Insights.Domain.Calendar;
using Parley.Insights.Features.Daily;
using Parley.Insights.Features.Priority.Build;
using Parley.Insights.Features.Rfm.Compute;
using Parley.Insights.Persistence;
using Parley.Insights.Shared;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Priority.Rebuild;

/// <summary>
/// Rebuilds the latest complete priority list of the organization against the scores and settings it has now. Queued when a
/// scoring run completes, when the ML switch changes and when the active uplift model changes. The rebuild takes the day's
/// lock, so it never interleaves with the daily run of that day; when that run holds the lock, the rebuild is queued again.
/// With no complete list yet (a fresh organization, or one whose first daily sweep has not run), it runs the first steps of the
/// organization's today as the daily job does: the RFM snapshot when it is missing, then the priority list. Each step is skipped
/// once its stamp exists, so a repeated rebuild changes nothing.
/// </summary>
public sealed class InsightsPriorityRebuildJob(
    IMutableTenantContext tenantContext,
    ITenantContext currentTenant,
    RfmSnapshotRunner rfm,
    PriorityBuildRunner priority,
    InsightsDbContext dbContext,
    InsightsDailyLock locks,
    IOrganizationDirectory organizations,
    PriorityRebuildScheduler scheduler,
    TimeProvider timeProvider) : TenantBackgroundJob(tenantContext)
{
    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var organizationId = currentTenant.OrganizationId;
        var latest = await DailyRunStamps.LatestPriorityDayAsync(dbContext, cancellationToken);
        var asOf = latest ?? await TodayAsync(organizationId, cancellationToken);

        await using var lease = await locks.TryAcquireAsync(organizationId, asOf, cancellationToken);
        if (lease is null)
        {
            scheduler.ScheduleAfter(organizationId, InsightsLimits.PriorityRebuildRetryDelay);
            return;
        }

        if (latest is null)
        {
            // The first list of the day: the same steps the daily job runs, the RFM snapshot first when it is missing.
            var rfmCompleted = await dbContext.DailyRuns.AsNoTracking()
                .AnyAsync(run => run.AsOf == asOf && run.RfmCompletedAt != null, cancellationToken);
            if (!rfmCompleted)
            {
                await rfm.RunAsync(asOf, cancellationToken);
            }
        }

        await priority.RunAsync(asOf, cancellationToken);
    }

    /// <summary>The organization's calendar day, as the daily sweep counts it. A zone the host does not know falls back to UTC.</summary>
    private async Task<DateOnly> TodayAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var instant = timeProvider.GetUtcNow();
        var organization = (await organizations.ListAsync(cancellationToken))
            .FirstOrDefault(row => row.OrganizationId == organizationId);
        return organization is null || !OrganizationCalendar.IsKnownZone(organization.TimeZone)
            ? DateOnly.FromDateTime(instant.UtcDateTime)
            : OrganizationCalendar.LocalDate(instant, organization.TimeZone);
    }
}
