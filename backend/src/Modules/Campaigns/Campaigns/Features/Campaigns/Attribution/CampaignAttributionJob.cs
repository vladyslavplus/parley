using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Attribution;

/// <summary>
/// Attributes one campaign under the organization of its job arguments. Enqueued by the daily sweep and by the on-demand
/// refresh. Idempotent: outcomes and results are upserted on their unique keys.
/// </summary>
public sealed class CampaignAttributionJob(IMutableTenantContext tenantContext, CampaignAttributionRunner runner)
    : TenantBackgroundJob(tenantContext)
{
    private Guid _campaignId;

    /// <summary>Entry point enqueued by the sweep and the refresh.</summary>
    public Task ExecuteAsync(Guid organizationId, Guid campaignId, CancellationToken cancellationToken)
    {
        _campaignId = campaignId;
        return ExecuteAsync(organizationId, cancellationToken);
    }

    protected override Task RunAsync(CancellationToken cancellationToken) =>
        runner.RunAsync(_campaignId, cancellationToken);
}
