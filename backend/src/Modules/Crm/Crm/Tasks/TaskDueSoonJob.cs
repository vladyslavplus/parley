using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Options;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Tasks;

/// <summary>
/// Publishes <c>TaskDueSoon</c> once for each open task whose due instant lies inside the reminder window. Runs
/// under the organization of its job arguments. The reminder is claimed with a conditional update in the same
/// transaction as the outbox row, so a task is announced once, and a re-run finds nothing left to announce.
/// </summary>
public sealed class TaskDueSoonJob(
    IMutableTenantContext tenantContext,
    CrmDbContext dbContext,
    IModuleOutbox<CrmDbContext> outbox,
    IOptions<CrmTaskOptions> options,
    TimeProvider timeProvider) : TenantBackgroundJob(tenantContext)
{
    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var windowEnd = now.Add(options.Value.DueSoonWindow).UtcDateTime;
        var due = await dbContext.Tasks
            .Where(task => task.CompletedAt == null && task.DeletedAt == null && task.ReminderSentAt == null
                && task.DueAt != null && task.DueAt <= windowEnd)
            .OrderBy(task => task.DueAt)
            .ThenBy(task => task.Id)
            .Take(CrmLimits.TaskDueSoonBatchSize)
            .Select(task => new { task.Id, task.AssigneeUserId, task.DueAt })
            .ToListAsync(cancellationToken);

        foreach (var task in due)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var claimed = await TaskReminderClaim.ClaimAsync(dbContext, task.Id, now.UtcDateTime, windowEnd, cancellationToken);
            if (claimed == 0)
            {
                continue;
            }

            outbox.Add(new TaskDueSoon
            {
                TaskId = task.Id,
                AssigneeUserId = task.AssigneeUserId,
                DueAt = task.DueAt!.Value,
                OccurredAt = now,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}
