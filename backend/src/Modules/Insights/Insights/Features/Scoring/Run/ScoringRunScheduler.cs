using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Parley.Insights.Options;

namespace Parley.Insights.Features.Scoring.Run;

/// <summary>Queues the scoring job of one run on the configured queue.</summary>
public sealed class ScoringRunScheduler(IBackgroundJobClient jobs, IOptions<InsightsScoringOptions> options)
{
    public void Schedule(Guid organizationId, Guid runId)
    {
        var job = Hangfire.Common.Job.FromExpression<RunScoringJob>(job => job.ExecuteAsync(organizationId, runId, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(options.Value.QueueName));
    }
}
