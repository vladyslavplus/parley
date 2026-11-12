using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Features.Campaigns.Start;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Dispatch.Job;

/// <summary>
/// Recurring sweep that queues one send job for each sending campaign that has work and no live runner. It covers a lost
/// enqueue and a worker that died mid-send. Re-queuing cannot send twice: claims are unique per recipient, and a claim whose
/// outcome is unknown is never sent again. The sweep runs platform-wide; each queued job runs under its own organization.
/// </summary>
public sealed class CampaignSendRecoveryJob(
    IMutableTenantContext tenantContext,
    CampaignsDbContext dbContext,
    CampaignSendScheduler scheduler,
    IOptions<CampaignsSendOptions> options,
    TimeProvider timeProvider) : TenantBackgroundJob(tenantContext)
{
    protected override bool PlatformWide => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var retryCutoff = now.Subtract(options.Value.RetryBackoff);
        var candidates = await dbContext.Database
            .SqlQuery<CampaignSendCandidate>(CampaignSendRecoverySql.Candidates(now, retryCutoff, options.Value.MaxAttempts))
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            scheduler.Schedule(candidate.OrganizationId, candidate.CampaignId);
        }
    }
}
