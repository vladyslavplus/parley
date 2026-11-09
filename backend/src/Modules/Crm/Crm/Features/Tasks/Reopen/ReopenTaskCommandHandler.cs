using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Tasks;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.Reopen;

public sealed class ReopenTaskCommandHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory) : IRequestHandler<ReopenTaskCommand, Result<TaskView>>
{
    public async ValueTask<Result<TaskView>> Handle(ReopenTaskCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.TasksWrite, cancellationToken);
        var task = await TaskLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (task is null)
        {
            return Result<TaskView>.Failure(Error.NotFound(CrmErrorCodes.TaskNotFound));
        }

        if (task.CompletedAt is null)
        {
            // Idempotent: reopening an open task changes nothing and returns the current task.
            return await CurrentViewAsync(task.Id, scope, cancellationToken);
        }

        task.CompletedAt = null;
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

        return await CurrentViewAsync(task.Id, scope, cancellationToken);
    }

    private async Task<Result<TaskView>> CurrentViewAsync(Guid taskId, DataScope scope, CancellationToken cancellationToken)
    {
        var row = await TaskLookup.FindRowAsync(dbContext, dataScope, taskId, scope, cancellationToken);
        return row is null
            ? Result<TaskView>.Failure(Error.NotFound(CrmErrorCodes.TaskNotFound))
            : Result<TaskView>.Success(await TaskViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
