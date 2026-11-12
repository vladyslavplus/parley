using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Parley.Identity.Contracts.Organizations;
using Parley.Insights.Domain.Calendar;
using Parley.Insights.Persistence;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Daily;

/// <summary>
/// Hourly platform-wide sweep: for each organization whose calendar day (in its own time zone) has no RFM snapshot yet,
/// queues the daily job of that day. An organization whose day is already computed is skipped, so a sweep that runs
/// every hour queues each organization once per local day.
/// </summary>
public sealed class InsightsDailySweepJob(
    IMutableTenantContext tenantContext,
    IOrganizationDirectory organizations,
    InsightsDbContext dbContext,
    InsightsDailyScheduler scheduler,
    TimeProvider timeProvider,
    ILogger<InsightsDailySweepJob> logger) : TenantBackgroundJob(tenantContext)
{
    protected override bool PlatformWide => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var instant = timeProvider.GetUtcNow();
        foreach (var organization in await organizations.ListAsync(cancellationToken))
        {
            if (!OrganizationCalendar.IsKnownZone(organization.TimeZone))
            {
                logger.LogWarning("Insights skipped organization {OrganizationId}: its time zone is not known to the host.", organization.OrganizationId);
                continue;
            }

            var day = OrganizationCalendar.LocalDate(instant, organization.TimeZone);

            // Queued again while either step of the day is missing: a failed priority step is retried, and a day whose
            // first run never finished is not left behind. The daily job itself skips the steps already stamped.
            var complete = await dbContext.DailyRuns.IgnoreQueryFilters()
                .AnyAsync(
                    run => run.OrganizationId == organization.OrganizationId
                        && run.AsOf == day
                        && run.RfmCompletedAt != null
                        && run.PriorityCompletedAt != null,
                    cancellationToken);
            if (!complete)
            {
                scheduler.Schedule(organization.OrganizationId, day);
            }
        }
    }
}
