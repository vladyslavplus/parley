using System.Linq.Expressions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Domain.Tasks;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Tasks;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Tasks.List;

public sealed class ListTasksQueryHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    ICurrentUser currentUser,
    OwnerDirectory ownerDirectory,
    TimeProvider timeProvider) : IRequestHandler<ListTasksQuery, Result<KeysetCursorPage<TaskView>>>
{
    public async ValueTask<Result<KeysetCursorPage<TaskView>>> Handle(ListTasksQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var descending = SortDirections.IsDescending(query.Order);
        var sort = query.Sort ?? TaskSortFields.Due;

        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var scope = await dataScope.ForPermissionAsync(Permissions.TasksRead, cancellationToken);
        var visibleContacts = await DealLookup.VisibleContactsAsync(dbContext, dataScope, cancellationToken);
        var visibleCompanies = await DealLookup.VisibleCompaniesAsync(dbContext, dataScope, cancellationToken);
        var visibleDeals = await DealLookup.VisibleDealsAsync(dbContext, dataScope, cancellationToken);
        var assignee = query.Mine ? currentUser.RequireUserId() : query.AssigneeUserId;
        var filter = new TaskListFilter(query.Search, assignee, query.ContactId, query.DealId, query.CompanyId, query.Status, timeProvider.GetUtcNow().UtcDateTime);
        var tasks = TaskListFilters.Apply(TaskScopeFilter.Apply(dbContext.Tasks.AsNoTracking(), scope), filter);
        var projection = TaskViews.RowProjectionFor(visibleContacts, visibleCompanies, visibleDeals);

        return sort switch
        {
            TaskSortFields.Due => await PageAsync(
                tasks, task => task.DueAt ?? TaskDueKeys.NoDueDate, projection, cursor, TaskSortFields.Due, KeysetOrdering.ParseTimestamp,
                row => row.DueAt ?? TaskDueKeys.NoDueDate, KeysetOrdering.FormatTimestamp, descending, limit, cancellationToken),
            TaskSortFields.Created => await PageAsync(
                tasks, task => task.CreatedAt, projection, cursor, TaskSortFields.Created, KeysetOrdering.ParseTimestamp,
                row => row.CreatedAt, KeysetOrdering.FormatTimestamp, descending, limit, cancellationToken),
            _ => await PageByTitleAsync(tasks, projection, cursor, descending, limit, cancellationToken),
        };
    }

    /// <summary>A page ordered by a timestamp key.</summary>
    private async Task<Result<KeysetCursorPage<TaskView>>> PageAsync<TKey>(
        IQueryable<CrmTask> tasks,
        Expression<Func<CrmTask, TKey>> key,
        Expression<Func<CrmTask, TaskRow>> projection,
        KeysetCursor? cursor,
        string sortField,
        Func<string, TKey?> parseKey,
        Func<TaskRow, TKey> keyOf,
        Func<TKey, string> formatKey,
        bool descending,
        int limit,
        CancellationToken cancellationToken)
        where TKey : struct
    {
        var position = KeysetOrdering.ReadValuePosition(cursor, sortField, parseKey);
        if (cursor is not null && position is null)
        {
            return Failure();
        }

        var rows = await KeysetOrdering.Apply<CrmTask, TKey>(tasks, key, task => task.Id, position, descending)
            .Select(projection)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        return await ToPageAsync(rows, limit, row => KeysetOrdering.CursorFor(sortField, formatKey(keyOf(row)), row.Id), cancellationToken);
    }

    /// <summary>A page ordered by the normalized title.</summary>
    private async Task<Result<KeysetCursorPage<TaskView>>> PageByTitleAsync(
        IQueryable<CrmTask> tasks,
        Expression<Func<CrmTask, TaskRow>> projection,
        KeysetCursor? cursor,
        bool descending,
        int limit,
        CancellationToken cancellationToken)
    {
        var position = cursor is null ? null : KeysetOrdering.ReadPosition(cursor, TaskSortFields.Title, KeysetOrdering.ParseText);
        if (cursor is not null && position is null)
        {
            return Failure();
        }

        var rows = await KeysetOrdering.Apply(tasks, task => task.TitleNormalized, task => task.Id, position, descending)
            .Select(projection)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        return await ToPageAsync(rows, limit, row => KeysetOrdering.CursorFor(TaskSortFields.Title, row.TitleNormalized, row.Id), cancellationToken);
    }

    private async Task<Result<KeysetCursorPage<TaskView>>> ToPageAsync(
        List<TaskRow> rows,
        int limit,
        Func<TaskRow, KeysetCursor> cursorOf,
        CancellationToken cancellationToken)
    {
        var page = KeysetCursorPage<TaskRow>.From(rows, limit, cursorOf);
        var assignees = await ownerDirectory.ReadAsync(page.Items.Select(row => (Guid?)row.AssigneeUserId), cancellationToken);
        var items = page.Items.Select(row => TaskViews.ToView(row, assignees)).ToArray();
        return Result<KeysetCursorPage<TaskView>>.Success(new KeysetCursorPage<TaskView>(items, page.NextCursor));
    }

    private static Result<KeysetCursorPage<TaskView>> Failure() =>
        Result<KeysetCursorPage<TaskView>>.Failure(CrmErrors.Field(nameof(ListTasksQuery.After), ErrorCodes.ValidationFailed));
}
