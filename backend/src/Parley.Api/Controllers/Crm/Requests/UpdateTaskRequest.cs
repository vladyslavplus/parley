namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a task update: the row version the client read, then the editable fields. A null assignee keeps the current one.</summary>
public sealed record UpdateTaskRequest(
    uint Version,
    string? Title,
    string? Notes,
    DateTimeOffset? DueAt,
    Guid? AssigneeUserId,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId);
