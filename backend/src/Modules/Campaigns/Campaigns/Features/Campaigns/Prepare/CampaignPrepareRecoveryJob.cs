using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>
/// Recurring sweep that queues the freeze of each campaign that has sat in <c>preparing</c> without a write for the
/// recovery age. It covers a lost enqueue and a worker that died mid-freeze. Queuing again cannot freeze twice: the freeze
/// takes a per-campaign lock, and a run that does not get it exits. Platform-wide; each queued job runs under its own
/// organization.
/// </summary>
public sealed class CampaignPrepareRecoveryJob(
    IMutableTenantContext tenantContext,
    CampaignsDbContext dbContext,
    CampaignPrepareScheduler scheduler,
    IOptions<CampaignsPrepareOptions> options,
    TimeProvider timeProvider) : TenantBackgroundJob(tenantContext)
{
    protected override bool PlatformWide => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().Subtract(options.Value.RecoveryAge).UtcDateTime;
        var stuck = await dbContext.Campaigns.IgnoreQueryFilters()
            .Where(campaign => campaign.Status == CampaignStatuses.Preparing
                && campaign.AudienceFrozenAt == null
                && campaign.UpdatedAt <= cutoff)
            .Select(campaign => new { campaign.OrganizationId, campaign.Id })
            .ToListAsync(cancellationToken);

        foreach (var campaign in stuck)
        {
            scheduler.Schedule(campaign.OrganizationId, campaign.Id);
        }
    }
}
