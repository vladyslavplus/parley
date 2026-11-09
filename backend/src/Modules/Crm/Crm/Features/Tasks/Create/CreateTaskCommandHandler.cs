using Mediator;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Domain.Tasks;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Subjects;
using Parley.Crm.Shared.Tasks;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Tasks.Create;

public sealed class CreateTaskCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    IPermissionChecker permissionChecker,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<CreateTaskCommand, Result<TaskView>>
{
    public async ValueTask<Result<TaskView>> Handle(CreateTaskCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();
        var scope = await dataScope.ForPermissionAsync(Permissions.TasksWrite, cancellationToken);

        var assigneeId = command.AssigneeUserId ?? actorId;
        if (!await OwnerRules.IsAllowedAsync(assigneeId, scope, organizationId, permissionChecker, cancellationToken))
        {
            return Result<TaskView>.Failure(CrmErrors.Field(nameof(CreateTaskCommand.AssigneeUserId), CrmErrorCodes.TaskAssigneeNotAllowed));
        }

        var unreadable = await SubjectChecks.FirstUnreadableAsync(dbContext, dataScope, command.ContactId, command.DealId, command.CompanyId, cancellationToken);
        if (unreadable is not null)
        {
            return Result<TaskView>.Failure(CrmErrors.Field(unreadable, CrmErrorCodes.TaskSubjectNotFound));
        }

        var now = timeProvider.GetUtcNow();
        var task = new CrmTask
        {
            OrganizationId = organizationId,
            Title = TextValues.Required(command.Title),
            Notes = TextValues.Optional(command.Notes),
            DueAt = command.DueAt?.UtcDateTime,
            AssigneeUserId = assigneeId,
            ContactId = command.ContactId,
            DealId = command.DealId,
            CompanyId = command.CompanyId,
        };
        dbContext.Tasks.Add(task);
        outbox.Add(new TaskAssigned { TaskId = task.Id, AssigneeUserId = assigneeId, OccurredAt = now });

        await dbContext.SaveChangesAsync(cancellationToken);

        var readScope = await dataScope.ForPermissionAsync(Permissions.TasksRead, cancellationToken);
        var row = await TaskLookup.FindRowAsync(dbContext, dataScope, task.Id, readScope, cancellationToken);
        return row is null
            ? Result<TaskView>.Failure(Error.NotFound(CrmErrorCodes.TaskNotFound))
            : Result<TaskView>.Success(await TaskViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
