using Parley.Crm.Domain.Tasks;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Authorization.Scope;

/// <summary>
/// Restricts task queries to the tasks the current user may see. A task is visible through its assignee;
/// a task outside the scope is reported as not found. Soft-deleted tasks are excluded unless asked for.
/// </summary>
public static class TaskScopeFilter
{
    public static IQueryable<CrmTask> Apply(IQueryable<CrmTask> tasks, DataScope scope, bool includeDeleted = false)
    {
        var rows = includeDeleted ? tasks : tasks.Where(task => task.DeletedAt == null);
        return scope.Level switch
        {
            DataScopeLevel.Organization => rows,
            DataScopeLevel.Own or DataScopeLevel.Team => ApplyAssignees(rows, scope.OwnerUserIds),
            _ => rows.Where(_ => false),
        };
    }

    private static IQueryable<CrmTask> ApplyAssignees(IQueryable<CrmTask> tasks, IReadOnlyCollection<Guid>? assigneeUserIds)
    {
        var assignees = assigneeUserIds?.ToList() ?? [];
        return tasks.Where(task => assignees.Contains(task.AssigneeUserId));
    }
}
