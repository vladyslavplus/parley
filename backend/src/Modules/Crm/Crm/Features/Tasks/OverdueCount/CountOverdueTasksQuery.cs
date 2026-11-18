using Mediator;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.OverdueCount;

/// <summary>
/// Counts the open tasks past their due instant that the caller may see, under the filters of the task list (the status
/// filter is fixed to overdue), so a list can show how many rows its overdue option holds.
/// </summary>
/// <param name="Search">Case-insensitive text the title must contain.</param>
/// <param name="AssigneeUserId">Only tasks assigned to this member.</param>
/// <param name="Mine">Only tasks assigned to the caller; takes the place of <paramref name="AssigneeUserId"/>.</param>
/// <param name="ContactId">Only tasks linked to this contact.</param>
/// <param name="DealId">Only tasks linked to this deal.</param>
/// <param name="CompanyId">Only tasks linked to this company.</param>
/// <param name="Type">Only tasks of this type: <c>call</c>, <c>email</c>, <c>meeting</c> or <c>todo</c>.</param>
[RequiresPermission(Permissions.TasksRead)]
public sealed record CountOverdueTasksQuery(
    string? Search,
    Guid? AssigneeUserId,
    bool Mine,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId,
    string? Type) : IRequest<Result<TaskOverdueCountView>>;
