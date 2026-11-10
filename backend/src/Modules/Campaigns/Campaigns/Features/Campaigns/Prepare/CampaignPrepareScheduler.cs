using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Options;

namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>Queues the prepare job of a campaign. Called after the preparing state commits.</summary>
public sealed class CampaignPrepareScheduler(IBackgroundJobClient jobs, IOptions<CampaignsPrepareOptions> options)
{
    public void Schedule(Guid organizationId, Guid campaignId)
    {
        var job = Hangfire.Common.Job.FromExpression<PrepareCampaignJob>(
            job => job.ExecuteAsync(organizationId, campaignId, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(options.Value.QueueName));
    }
}
