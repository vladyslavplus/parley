using System.Text.Json;

namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of company create. The owner defaults to the caller when omitted.</summary>
public sealed record CompanyRequest(
    string? Name,
    string? Domain,
    string? Country,
    string? Industry,
    Guid? OwnerUserId,
    IReadOnlyDictionary<string, JsonElement>? CustomFields);
