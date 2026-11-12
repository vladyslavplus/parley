using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Parley.Insights.Options;

namespace Parley.Insights.Features.Scoring.Models;

/// <summary>Queues a platform-wide model sync on the configured queue.</summary>
public sealed class ScoringModelsSyncScheduler(IBackgroundJobClient jobs, IOptions<InsightsJobOptions> options)
{
    public void Schedule()
    {
        var job = Hangfire.Common.Job.FromExpression<ScoringModelsSyncJob>(job => job.ExecuteAsync(null, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(options.Value.QueueName));
    }
}
