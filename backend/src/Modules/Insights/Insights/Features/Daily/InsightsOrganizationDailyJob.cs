using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Parley.Insights.Features.Priority.Build;
using Parley.Insights.Features.Rfm.Compute;
using Parley.Insights.Persistence;
using Parley.Insights.Shared;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Daily;

/// <summary>
/// The daily work of one organization: the RFM snapshot of its day, then the priority list of that day. Enqueued by the
/// sweep. The run takes the day's lock first, so two runs of one day never interleave, and each step runs only while its
/// completion stamp is missing. A retry therefore resumes at the first step that did not finish.
/// </summary>
public sealed class InsightsOrganizationDailyJob(
    IMutableTenantContext tenantContext,
    ITenantContext currentTenant,
    RfmSnapshotRunner rfm,
    PriorityBuildRunner priority,
    InsightsDbContext dbContext,
    InsightsDailyLock locks,
    ILogger<InsightsOrganizationDailyJob> logger) : TenantBackgroundJob(tenantContext)
{
    private DateOnly _asOf;

    /// <summary>Entry point enqueued by the sweep. The day travels as text, because the job arguments are serialized as JSON.</summary>
    public Task ExecuteAsync(Guid organizationId, string asOfDay, CancellationToken cancellationToken)
    {
        _asOf = DateOnly.ParseExact(asOfDay, InsightsLimits.DayFormat, CultureInfo.InvariantCulture);
        return ExecuteAsync(organizationId, cancellationToken);
    }

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var lease = await locks.TryAcquireAsync(currentTenant.OrganizationId, _asOf, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("The insights run of day {AsOf} is already in progress for this organization; this run left.", _asOf);
            return;
        }

        var stamps = await dbContext.DailyRuns.AsNoTracking()
            .Where(run => run.AsOf == _asOf)
            .Select(run => new { run.RfmCompletedAt, run.PriorityCompletedAt })
            .FirstOrDefaultAsync(cancellationToken);

        // Each step stamps itself when it finishes, so a failure leaves its stamp missing for the next run.
        if (stamps?.RfmCompletedAt is null)
        {
            await rfm.RunAsync(_asOf, cancellationToken);
        }

        if (stamps?.PriorityCompletedAt is null)
        {
            await priority.RunAsync(_asOf, cancellationToken);
        }
    }
}
