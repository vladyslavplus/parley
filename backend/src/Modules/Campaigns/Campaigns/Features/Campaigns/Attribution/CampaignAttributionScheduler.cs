using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Options;

namespace Parley.Campaigns.Features.Campaigns.Attribution;

/// <summary>Queues the attribution job of a campaign: now, or at the instant its window closes, so the final figures do not wait for the sweep.</summary>
public sealed class CampaignAttributionScheduler(IBackgroundJobClient jobs, IOptions<CampaignsAttributionOptions> options)
{
    /// <summary>Margin after the window end, so the run's clock is certainly past the end when it stamps the figures.</summary>
    public static readonly TimeSpan FinalRunMargin = TimeSpan.FromMinutes(1);

    public void Schedule(Guid organizationId, Guid campaignId)
    {
        var job = Hangfire.Common.Job.FromExpression<CampaignAttributionJob>(
            job => job.ExecuteAsync(organizationId, campaignId, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(options.Value.QueueName));
    }

    /// <summary>
    /// Queues the attribution run that computes the final figures, at the window end plus the margin, on the same queue as
    /// the other attribution runs. The queue is passed with the schedule: a bare scheduled job would fall back to the
    /// default queue when it fires.
    /// </summary>
    public void ScheduleFinal(Guid organizationId, Guid campaignId, DateTimeOffset windowEndsAt)
    {
        jobs.Schedule<CampaignAttributionJob>(
            options.Value.QueueName,
            job => job.ExecuteAsync(organizationId, campaignId, CancellationToken.None),
            windowEndsAt + FinalRunMargin);
    }
}
