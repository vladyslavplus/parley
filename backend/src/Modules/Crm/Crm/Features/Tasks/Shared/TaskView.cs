namespace Parley.Crm.Features.Tasks.Shared;

/// <summary>
/// A task as the API returns it. <see cref="Version"/> is the row version: clients send it back on update, complete
/// and reopen, and a stale value returns 409.
/// </summary>
/// <param name="Id">Task identifier.</param>
/// <param name="Title">Task title.</param>
/// <param name="Notes">Free text, when set.</param>
/// <param name="DueAt">Due instant in UTC, or null when the task has no due date.</param>
/// <param name="CompletedAt">Instant the task was completed in UTC, or null while open.</param>
/// <param name="IsCompleted">True when <paramref name="CompletedAt"/> is set.</param>
/// <param name="AssigneeUserId">Member the task is assigned to.</param>
/// <param name="AssigneeDisplayName">Assignee display name, or null when unnamed or outside the organization.</param>
/// <param name="AssigneeEmail">Assignee email, or null under the same conditions.</param>
/// <param name="ContactId">Linked contact, when set.</param>
/// <param name="ContactName">Full name of the linked contact, or null when unset or outside the caller's scope.</param>
/// <param name="DealId">Linked deal, when set.</param>
/// <param name="DealTitle">Title of the linked deal, or null when unset or outside the caller's scope.</param>
/// <param name="CompanyId">Linked company, when set.</param>
/// <param name="CompanyName">Name of the linked company, or null when unset or outside the caller's scope.</param>
/// <param name="CreatedAt">Creation instant in UTC.</param>
/// <param name="UpdatedAt">Last change instant in UTC.</param>
/// <param name="Version">Row version to send back on update, complete or reopen.</param>
public sealed record TaskView(
    Guid Id,
    string Title,
    string? Notes,
    DateTime? DueAt,
    DateTime? CompletedAt,
    bool IsCompleted,
    Guid AssigneeUserId,
    string? AssigneeDisplayName,
    string? AssigneeEmail,
    Guid? ContactId,
    string? ContactName,
    Guid? DealId,
    string? DealTitle,
    Guid? CompanyId,
    string? CompanyName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version);
