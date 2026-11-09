using Mediator;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tasks.Update;

/// <summary>
/// Replaces the editable fields of a task. <paramref name="Version"/> is the row version the client read; a stale value
/// returns 409. A null assignee keeps the current one. Changing the due instant re-arms the due-soon reminder. Emits
/// <c>TaskAssigned</c> when the assignee changes.
/// </summary>
/// <param name="Id">Task identifier.</param>
/// <param name="Version">Row version the client read.</param>
/// <param name="Title">Task title.</param>
/// <param name="Notes">Free text, or null to clear it.</param>
/// <param name="DueAt">Due instant, or null to clear it.</param>
/// <param name="AssigneeUserId">New assignee, or null to keep the current one.</param>
/// <param name="ContactId">Linked contact, or null to unlink.</param>
/// <param name="DealId">Linked deal, or null to unlink.</param>
/// <param name="CompanyId">Linked company, or null to unlink.</param>
[RequiresPermission(Permissions.TasksWrite)]
public sealed record UpdateTaskCommand(
    Guid Id,
    uint Version,
    string Title,
    string? Notes,
    DateTimeOffset? DueAt,
    Guid? AssigneeUserId,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId) : IRequest<Result<TaskView>>;
