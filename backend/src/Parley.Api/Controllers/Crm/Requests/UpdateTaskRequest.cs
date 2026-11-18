namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a task update: the row version the client read, then the editable fields. A null assignee or type keeps the current one.</summary>
public sealed record UpdateTaskRequest(
    uint Version,
    string? Title,
    string? Type,
    string? Notes,
    DateTimeOffset? DueAt,
    Guid? AssigneeUserId,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId);
