using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Attribution;

/// <summary>
/// Daily sweep: queues one attribution job for each completed campaign of each organization whose final figures are not
/// computed yet. Platform-wide. A campaign whose result was computed at or after its window end is final and drops out of
/// the sweep, so the sweep does not re-queue finished campaigns forever. Running it again changes nothing, because the
/// attribution job upserts on unique keys.
/// </summary>
public sealed class CampaignAttributionSweepJob(
    IMutableTenantContext tenantContext,
    CampaignsDbContext dbContext,
    CampaignAttributionScheduler scheduler) : TenantBackgroundJob(tenantContext)
{
    protected override bool PlatformWide => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var finalCampaigns = dbContext.CampaignResults.IgnoreQueryFilters()
            .Where(result => result.ComputedAt >= result.WindowEndsAt)
            .Select(result => result.CampaignId);

        var pending = await dbContext.Campaigns.IgnoreQueryFilters()
            .Where(campaign => campaign.Status == CampaignStatuses.Completed && !finalCampaigns.Contains(campaign.Id))
            .Select(campaign => new { campaign.OrganizationId, campaign.Id })
            .ToListAsync(cancellationToken);

        foreach (var campaign in pending)
        {
            scheduler.Schedule(campaign.OrganizationId, campaign.Id);
        }
    }
}
