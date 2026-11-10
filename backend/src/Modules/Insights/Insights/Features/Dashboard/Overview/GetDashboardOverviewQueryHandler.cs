using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.Results;
using Parley.Crm.Contracts.Dashboard;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Options;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Dashboard.Overview;

/// <summary>
/// Builds the dashboard aggregate from the CRM and campaign readers, which return totals only. The answer is cached per
/// organization, user and visibility for the configured number of seconds; the key carries the visibility, so a cached
/// answer never reaches a caller with a wider or narrower scope.
/// </summary>
public sealed class GetDashboardOverviewQueryHandler(
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IDataScope dataScope,
    ICrmDashboardReader crmDashboard,
    ICampaignResultsReader campaignResults,
    IMemoryCache cache,
    IOptions<InsightsDashboardOptions> options,
    TimeProvider timeProvider) : IRequestHandler<GetDashboardOverviewQuery, Result<DashboardOverviewView>>
{
    public async ValueTask<Result<DashboardOverviewView>> Handle(GetDashboardOverviewQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var organizationId = tenantContext.OrganizationId;
        var deals = await dataScope.ForPermissionAsync(Permissions.DealsRead, cancellationToken);
        var tasks = await dataScope.ForPermissionAsync(Permissions.TasksRead, cancellationToken);
        var campaignsVisible = await permissions.HasPermissionAsync(userId, organizationId, Permissions.CampaignsRead, cancellationToken);

        var key = DashboardCacheKeys.For(organizationId, userId, deals.Level, tasks.Level, campaignsVisible);
        if (cache.TryGetValue(key, out DashboardOverviewView? cached) && cached is not null)
        {
            return Result<DashboardOverviewView>.Success(cached);
        }

        var stages = (await crmDashboard.ReadOpenPipelineAsync(deals.OwnerUserIds, cancellationToken))
            .Select(stage => new DashboardPipelineStageView(stage.StageId, stage.StageName, stage.Position, stage.DealCount, stage.Amount))
            .ToList();
        var tasksDue = await crmDashboard.CountTasksDueAsync(timeProvider.GetUtcNow(), tasks.OwnerUserIds, cancellationToken);

        LatestCampaignView? latest = null;
        if (campaignsVisible)
        {
            var summary = await campaignResults.FindLatestCompletedAsync(cancellationToken);
            if (summary is not null)
            {
                latest = new LatestCampaignView(
                    summary.CampaignId,
                    summary.Name,
                    summary.Source,
                    summary.Verdict,
                    summary.Simulated,
                    summary.WindowState,
                    summary.IsFinal,
                    summary.CompletedAt,
                    summary.Lift,
                    summary.LiftCiLow,
                    summary.LiftCiHigh);
            }
        }

        var view = new DashboardOverviewView(
            stages,
            stages.Sum(stage => stage.Amount),
            stages.Sum(stage => stage.DealCount),
            tasksDue,
            latest);
        cache.Set(key, view, TimeSpan.FromSeconds(options.Value.CacheSeconds));
        return Result<DashboardOverviewView>.Success(view);
    }
}
