using System.Text.Json;

namespace Parley.Crm.Features.Companies.Shared;

/// <summary>
/// A company as the API returns it. <see cref="Version"/> is the row version: clients send it
/// back as <c>version</c> on update, and a stale value returns 409.
/// </summary>
/// <param name="Id">Company identifier.</param>
/// <param name="Name">Company name.</param>
/// <param name="Domain">Web domain, when known.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code, when known.</param>
/// <param name="Industry">Industry, when known.</param>
/// <param name="OwnerUserId">Owner, or null when unowned.</param>
/// <param name="CustomFields">Custom field values keyed by definition key.</param>
/// <param name="CreatedAt">Creation instant in UTC.</param>
/// <param name="UpdatedAt">Last change instant in UTC.</param>
/// <param name="Version">Row version to send back on update.</param>
public sealed record CompanyView(
    Guid Id,
    string Name,
    string? Domain,
    string? Country,
    string? Industry,
    Guid? OwnerUserId,
    IReadOnlyDictionary<string, JsonElement> CustomFields,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version);
