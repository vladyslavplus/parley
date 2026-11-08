using Hangfire;
using Hangfire.States;
using Microsoft.Extensions.Options;
using Parley.Crm.Options;

namespace Parley.Crm.Exports;

/// <summary>
/// Queues contact export jobs. Called after the export row commits, so the job always finds
/// its row. The job is idempotent: a second run of a completed export does nothing.
/// </summary>
public sealed class ContactExportScheduler(IBackgroundJobClient jobs, IOptions<CrmExportOptions> options)
{
    public void Schedule(Guid organizationId, Guid exportId)
    {
        var job = Hangfire.Common.Job.FromExpression<ContactExportJob>(
            job => job.ExecuteAsync(organizationId, exportId, CancellationToken.None));
        jobs.Create(job, new EnqueuedState(options.Value.QueueName));
    }
}
