using Mediator;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Tasks.Delete;

public sealed class DeleteTaskCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<DeleteTaskCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteTaskCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.TasksDelete, cancellationToken);
        var task = await TaskLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (task is null)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.TaskNotFound));
        }

        task.DeletedAt = timeProvider.GetUtcNow().UtcDateTime;
        outbox.AddAuditRecorded(currentUser.RequireUserId(), tenantContext.OrganizationId, CrmAuditActions.TaskDeleted, timeProvider, CrmAuditTargetTypes.Task, task.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
