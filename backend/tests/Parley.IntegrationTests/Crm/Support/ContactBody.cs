using System.Text.Json;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>Contact create body sent by the tests. Serialized with the API's JSON defaults.</summary>
public sealed record ContactBody(
    string FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Country,
    string? City,
    Guid? CompanyId,
    Guid? OwnerUserId,
    IReadOnlyDictionary<string, JsonElement>? CustomFields);
