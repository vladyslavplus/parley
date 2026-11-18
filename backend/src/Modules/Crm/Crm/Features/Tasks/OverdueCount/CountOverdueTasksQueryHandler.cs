using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Tasks;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Tasks.OverdueCount;

public sealed class CountOverdueTasksQueryHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<CountOverdueTasksQuery, Result<TaskOverdueCountView>>
{
    public async ValueTask<Result<TaskOverdueCountView>> Handle(CountOverdueTasksQuery query, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.TasksRead, cancellationToken);
        var assignee = query.Mine ? currentUser.RequireUserId() : query.AssigneeUserId;
        var filter = new TaskListFilter(
            query.Search,
            assignee,
            query.ContactId,
            query.DealId,
            query.CompanyId,
            TaskStatuses.Overdue,
            query.Type,
            timeProvider.GetUtcNow().UtcDateTime);
        var count = await TaskListFilters.Apply(TaskScopeFilter.Apply(dbContext.Tasks.AsNoTracking(), scope), filter)
            .CountAsync(cancellationToken);
        return Result<TaskOverdueCountView>.Success(new TaskOverdueCountView(count));
    }
}
