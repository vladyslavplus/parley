using System.Text.Json;

namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>
/// Body of contact update. <c>version</c> is the row version read from the contact; a stale value
/// returns 409. The owner changes through the bulk owner endpoint.
/// </summary>
public sealed record UpdateContactRequest(
    uint? Version,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Country,
    string? City,
    Guid? CompanyId,
    IReadOnlyDictionary<string, JsonElement>? CustomFields);
