using Mediator;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.List;

/// <summary>
/// Lists the tasks the caller may see, keyset-paged. Ties on the sort value are broken by identifier, so every row
/// appears exactly once across pages.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Search">Case-insensitive text the title must contain.</param>
/// <param name="AssigneeUserId">Only tasks assigned to this member.</param>
/// <param name="Mine">Only tasks assigned to the caller; takes the place of <paramref name="AssigneeUserId"/>.</param>
/// <param name="ContactId">Only tasks linked to this contact.</param>
/// <param name="DealId">Only tasks linked to this deal.</param>
/// <param name="CompanyId">Only tasks linked to this company.</param>
/// <param name="Status">Only tasks with this status: <c>open</c>, <c>completed</c> or <c>overdue</c>.</param>
/// <param name="Sort">Sort field: <c>due</c> (default), <c>title</c> or <c>created</c>.</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.TasksRead)]
public sealed record ListTasksQuery(
    string? After,
    int? Limit,
    string? Search,
    Guid? AssigneeUserId,
    bool Mine,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId,
    string? Status,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<TaskView>>>;
