using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Features.Campaigns.Dispatch.Job;
using Parley.Campaigns.Options;

namespace Parley.Campaigns.Features.Campaigns.Start;

/// <summary>Queues the send job of a campaign. Called after the sending state commits.</summary>
public sealed class CampaignSendScheduler(IBackgroundJobClient jobs, IOptions<CampaignsSendOptions> options)
{
    public void Schedule(Guid organizationId, Guid campaignId)
    {
        var job = Hangfire.Common.Job.FromExpression<CampaignSendJob>(
            job => job.ExecuteAsync(organizationId, campaignId, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(options.Value.QueueName));
    }
}
