using System.Globalization;
using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Parley.Insights.Options;
using Parley.Insights.Shared;

namespace Parley.Insights.Features.Daily;

/// <summary>Queues the daily job of one organization for one of its calendar days.</summary>
public sealed class InsightsDailyScheduler(IBackgroundJobClient jobs, IOptions<InsightsJobOptions> options)
{
    public void Schedule(Guid organizationId, DateOnly asOf)
    {
        var asOfDay = asOf.ToString(InsightsLimits.DayFormat, CultureInfo.InvariantCulture);
        var job = Hangfire.Common.Job.FromExpression<InsightsOrganizationDailyJob>(
            job => job.ExecuteAsync(organizationId, asOfDay, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(options.Value.QueueName));
    }
}
