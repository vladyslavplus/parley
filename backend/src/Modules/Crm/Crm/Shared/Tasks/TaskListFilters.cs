using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Tasks;
using Parley.SharedKernel.Search;

namespace Parley.Crm.Shared.Tasks;

/// <summary>Applies <see cref="TaskListFilter"/> to a task query.</summary>
public static class TaskListFilters
{
    public static IQueryable<CrmTask> Apply(IQueryable<CrmTask> tasks, TaskListFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            tasks = tasks.Where(task => EF.Functions.ILike(task.TitleNormalized, pattern));
        }

        if (filter.AssigneeUserId is { } assigneeUserId)
        {
            tasks = tasks.Where(task => task.AssigneeUserId == assigneeUserId);
        }

        if (filter.ContactId is { } contactId)
        {
            tasks = tasks.Where(task => task.ContactId == contactId);
        }

        if (filter.DealId is { } dealId)
        {
            tasks = tasks.Where(task => task.DealId == dealId);
        }

        if (filter.CompanyId is { } companyId)
        {
            tasks = tasks.Where(task => task.CompanyId == companyId);
        }

        return filter.Status switch
        {
            TaskStatuses.Open => tasks.Where(task => task.CompletedAt == null),
            TaskStatuses.Completed => tasks.Where(task => task.CompletedAt != null),
            TaskStatuses.Overdue => tasks.Where(task => task.CompletedAt == null && task.DueAt != null && task.DueAt < filter.Now),
            _ => tasks,
        };
    }
}
