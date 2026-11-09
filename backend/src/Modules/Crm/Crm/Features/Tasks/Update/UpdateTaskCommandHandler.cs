using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Subjects;
using Parley.Crm.Shared.Tasks;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Tasks.Update;

public sealed class UpdateTaskCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    IPermissionChecker permissionChecker,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<UpdateTaskCommand, Result<TaskView>>
{
    public async ValueTask<Result<TaskView>> Handle(UpdateTaskCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.TasksWrite, cancellationToken);
        var task = await TaskLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (task is null)
        {
            return Result<TaskView>.Failure(Error.NotFound(CrmErrorCodes.TaskNotFound));
        }

        var assigneeId = command.AssigneeUserId ?? task.AssigneeUserId;
        var assigneeChanged = assigneeId != task.AssigneeUserId;
        if (assigneeChanged
            && !await OwnerRules.IsAllowedAsync(assigneeId, scope, tenantContext.OrganizationId, permissionChecker, cancellationToken))
        {
            return Result<TaskView>.Failure(CrmErrors.Field(nameof(UpdateTaskCommand.AssigneeUserId), CrmErrorCodes.TaskAssigneeNotAllowed));
        }

        var unreadable = await SubjectChecks.FirstUnreadableAsync(dbContext, dataScope, command.ContactId, command.DealId, command.CompanyId, cancellationToken);
        if (unreadable is not null)
        {
            return Result<TaskView>.Failure(CrmErrors.Field(unreadable, CrmErrorCodes.TaskSubjectNotFound));
        }

        var dueAt = command.DueAt?.UtcDateTime;
        if (dueAt != task.DueAt)
        {
            // A new due instant gets its own reminder.
            task.ReminderSentAt = null;
        }

        task.Title = TextValues.Required(command.Title);
        task.Notes = TextValues.Optional(command.Notes);
        task.DueAt = dueAt;
        task.AssigneeUserId = assigneeId;
        task.ContactId = command.ContactId;
        task.DealId = command.DealId;
        task.CompanyId = command.CompanyId;
        if (assigneeChanged)
        {
            outbox.Add(new TaskAssigned { TaskId = task.Id, AssigneeUserId = assigneeId, OccurredAt = timeProvider.GetUtcNow() });
        }

        // The stale-write check: the UPDATE matches the client's row version, so a changed row affects nothing.
        var entry = dbContext.Entry(task);
        entry.Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        entry.State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<TaskView>.Failure(ConcurrencyErrors.ToConflictError());
        }

        var row = await TaskLookup.FindRowAsync(dbContext, dataScope, task.Id, scope, cancellationToken);
        return row is null
            ? Result<TaskView>.Failure(Error.NotFound(CrmErrorCodes.TaskNotFound))
            : Result<TaskView>.Success(await TaskViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
