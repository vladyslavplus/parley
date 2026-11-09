using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Tasks;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Tasks.Complete;

/// <summary>
/// Completes an open task in one transaction with its timeline entry. Completing a task that is already completed is
/// idempotent: nothing changes, no second entry is written, and the current task is returned with 200.
/// </summary>
public sealed class CompleteTaskCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    TimeProvider timeProvider) : IRequestHandler<CompleteTaskCommand, Result<TaskView>>
{
    public async ValueTask<Result<TaskView>> Handle(CompleteTaskCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.TasksWrite, cancellationToken);
        var task = await TaskLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (task is null)
        {
            return Result<TaskView>.Failure(Error.NotFound(CrmErrorCodes.TaskNotFound));
        }

        if (task.CompletedAt is not null)
        {
            // Idempotent: a repeat completion changes nothing and returns the current task.
            return await CurrentViewAsync(task.Id, scope, cancellationToken);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        task.CompletedAt = now;
        if (task.ContactId is not null || task.DealId is not null || task.CompanyId is not null)
        {
            dbContext.Activities.Add(new Activity
            {
                OrganizationId = tenantContext.OrganizationId,
                Type = ActivityTypes.TaskCompleted,
                ContactId = task.ContactId,
                DealId = task.DealId,
                CompanyId = task.CompanyId,
                TaskId = task.Id,
                ActorUserId = currentUser.RequireUserId(),
                OccurredAt = now,
            });
        }

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
