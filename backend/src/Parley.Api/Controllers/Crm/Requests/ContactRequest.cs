using System.Text.Json;

namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of contact create. The owner defaults to the caller when omitted.</summary>
public sealed record ContactRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Country,
    string? City,
    Guid? CompanyId,
    Guid? OwnerUserId,
    IReadOnlyDictionary<string, JsonElement>? CustomFields);
