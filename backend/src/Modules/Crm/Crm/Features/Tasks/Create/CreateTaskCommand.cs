using Mediator;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.Create;

/// <summary>
/// Creates a task. The assignee defaults to the caller and must lie inside the caller's scope. Emits <c>TaskAssigned</c>.
/// </summary>
/// <param name="Title">Task title.</param>
/// <param name="Notes">Free text, when given.</param>
/// <param name="DueAt">Due instant, or null for no due date.</param>
/// <param name="AssigneeUserId">Assignee; the caller when null.</param>
/// <param name="ContactId">Linked contact, when given.</param>
/// <param name="DealId">Linked deal, when given.</param>
/// <param name="CompanyId">Linked company, when given.</param>
[RequiresPermission(Permissions.TasksWrite)]
public sealed record CreateTaskCommand(
    string Title,
    string? Notes,
    DateTimeOffset? DueAt,
    Guid? AssigneeUserId,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId) : IRequest<Result<TaskView>>;
