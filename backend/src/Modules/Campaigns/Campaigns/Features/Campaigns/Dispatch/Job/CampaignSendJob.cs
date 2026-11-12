using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Dispatch.Job;

/// <summary>
/// Sends the claimable email of one campaign under the organization of its job arguments. Idempotent by construction:
/// claims are unique per recipient, so a re-run after a crash claims only what was never claimed, and a claim that
/// was in flight becomes <c>unknown</c> instead of being sent again.
/// </summary>
public sealed class CampaignSendJob(IMutableTenantContext tenantContext, CampaignSendRunner runner)
    : TenantBackgroundJob(tenantContext)
{
    private Guid _campaignId;

    /// <summary>Entry point enqueued by the scheduler.</summary>
    public Task ExecuteAsync(Guid organizationId, Guid campaignId, CancellationToken cancellationToken)
    {
        _campaignId = campaignId;
        return ExecuteAsync(organizationId, cancellationToken);
    }

    protected override Task RunAsync(CancellationToken cancellationToken) =>
        runner.RunAsync(_campaignId, cancellationToken);
}
