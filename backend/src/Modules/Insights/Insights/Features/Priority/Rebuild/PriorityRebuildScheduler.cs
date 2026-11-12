using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Parley.Insights.Options;

namespace Parley.Insights.Features.Priority.Rebuild;

/// <summary>Queues the rebuild of the latest priority list of one organization, now or after a delay.</summary>
public sealed class PriorityRebuildScheduler(IBackgroundJobClient jobs, IOptions<InsightsJobOptions> options)
{
    /// <summary>Queues a rebuild on the configured queue.</summary>
    public void Schedule(Guid organizationId)
    {
        var job = Hangfire.Common.Job.FromExpression<InsightsPriorityRebuildJob>(
            job => job.ExecuteAsync(organizationId, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(options.Value.QueueName));
    }

    /// <summary>Queues a rebuild that runs after <paramref name="delay"/>, used when the day was busy with another run.</summary>
    public void ScheduleAfter(Guid organizationId, TimeSpan delay)
    {
        var job = Hangfire.Common.Job.FromExpression<InsightsPriorityRebuildJob>(
            job => job.ExecuteAsync(organizationId, CancellationToken.None));
        jobs.Create(job, new ScheduledState(delay));
    }
}
