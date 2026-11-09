namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a task create: title, optional notes and due instant, assignee (the caller when absent) and links.</summary>
public sealed record TaskRequest(
    string? Title,
    string? Notes,
    DateTimeOffset? DueAt,
    Guid? AssigneeUserId,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId);
