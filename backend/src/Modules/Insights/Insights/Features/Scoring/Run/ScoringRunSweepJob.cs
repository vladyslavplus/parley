using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Options;
using Parley.Insights.Persistence;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Scoring.Run;

/// <summary>
/// Recurring sweep that fails scoring runs stuck in queued or running past <see cref="InsightsScoringOptions.RunStaleAfter"/>. A
/// lost job or a failed failure write would otherwise hold the kind's single active run forever. The stale runs are found across
/// the organizations by id; each one is then failed inside its own organization's tenant.
/// </summary>
public sealed class ScoringRunSweepJob(
    IMutableTenantContext tenantContext,
    InsightsDbContext dbContext,
    ScoringRunAbandonment abandonment,
    IOptions<InsightsScoringOptions> options,
    TimeProvider timeProvider) : TenantBackgroundJob(tenantContext)
{
    protected override bool PlatformWide => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var staleBefore = now - options.Value.RunStaleAfter;
        var stale = await dbContext.ScoringRuns.IgnoreQueryFilters().AsNoTracking()
            .Where(run => ScoringRunStatuses.Active.Contains(run.Status) && run.StartedAt < staleBefore)
            .Select(run => new { run.Id, run.OrganizationId })
            .ToListAsync(cancellationToken);

        foreach (var run in stale)
        {
            TenantContext.Set(run.OrganizationId);
            try
            {
                await abandonment.FailAsync(run.Id, now, cancellationToken);
            }
            finally
            {
                TenantContext.Clear();
            }
        }
    }
}
