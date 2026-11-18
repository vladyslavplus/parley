namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>
/// Body of a task create: title, optional type (<c>call</c>, <c>email</c>, <c>meeting</c> or <c>todo</c>; <c>todo</c> when absent),
/// notes and due instant, assignee (the caller when absent) and links.
/// </summary>
public sealed record TaskRequest(
    string? Title,
    string? Type,
    string? Notes,
    DateTimeOffset? DueAt,
    Guid? AssigneeUserId,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId);
