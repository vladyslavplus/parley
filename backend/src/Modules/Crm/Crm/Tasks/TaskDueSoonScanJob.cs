using Hangfire;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Crm.Options;
using Parley.Crm.Persistence;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Tasks;

/// <summary>
/// Recurring sweep that finds the organizations with open tasks inside the reminder window and queues one
/// <see cref="TaskDueSoonJob"/> for each. Platform-wide; each queued job then runs under its own organization.
/// Re-queuing is harmless because the reminder itself is claimed once.
/// </summary>
public sealed class TaskDueSoonScanJob(
    IMutableTenantContext tenantContext,
    CrmDbContext dbContext,
    IBackgroundJobClient jobs,
    IOptions<CrmTaskOptions> options,
    TimeProvider timeProvider) : TenantBackgroundJob(tenantContext)
{
    protected override bool PlatformWide => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var windowEnd = timeProvider.GetUtcNow().Add(options.Value.DueSoonWindow).UtcDateTime;
        var organizations = await dbContext.Tasks.IgnoreQueryFilters()
            .Where(task => task.CompletedAt == null && task.DeletedAt == null && task.ReminderSentAt == null
                && task.DueAt != null && task.DueAt <= windowEnd)
            .Select(task => task.OrganizationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var organizationId in organizations)
        {
            var job = Hangfire.Common.Job.FromExpression<TaskDueSoonJob>(
                job => job.ExecuteAsync(organizationId, CancellationToken.None));
            jobs.Create(job, new EnqueuedState(options.Value.QueueName));
        }
    }
}
