using System.Text.Json;

namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>
/// Body of company update. <c>version</c> is the row version read from the company; a stale value
/// returns 409. The owner changes through the bulk owner endpoint.
/// </summary>
public sealed record UpdateCompanyRequest(
    uint? Version,
    string? Name,
    string? Domain,
    string? Country,
    string? Industry,
    IReadOnlyDictionary<string, JsonElement>? CustomFields);
