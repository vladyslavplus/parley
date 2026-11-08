namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Filters of a contact export request. Every field is optional; an empty body exports all visible contacts.</summary>
public sealed record RequestContactExportRequest(
    string? Search,
    Guid? TagId,
    Guid? CompanyId,
    Guid? OwnerUserId,
    DateOnly? CreatedFrom,
    DateOnly? CreatedTo);
