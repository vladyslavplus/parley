using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>
/// Freezes the audience of a campaign under the organization of its job arguments. Idempotent: a campaign that is
/// already frozen, or that left <c>preparing</c>, is left alone, and a resumed run keeps the rows already written.
/// </summary>
public sealed class PrepareCampaignJob(IMutableTenantContext tenantContext, CampaignAudienceFreezer freezer)
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
        freezer.FreezeAsync(_campaignId, cancellationToken);
}
