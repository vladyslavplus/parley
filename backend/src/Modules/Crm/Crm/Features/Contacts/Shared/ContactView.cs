using System.Text.Json;

namespace Parley.Crm.Features.Contacts.Shared;

/// <summary>
/// A contact as the API returns it. <see cref="Version"/> is the row version: clients send it
/// back as <c>version</c> on update, and a stale value returns 409.
/// </summary>
/// <param name="Id">Contact identifier.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name, when known.</param>
/// <param name="Email">Email address, when known.</param>
/// <param name="Phone">Phone number, when known.</param>
/// <param name="JobTitle">Job title, when known.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code, when known.</param>
/// <param name="City">City, when known.</param>
/// <param name="CompanyId">Company the contact belongs to, when set.</param>
/// <param name="OwnerUserId">Owner, or null when unowned.</param>
/// <param name="TagIds">Identifiers of the attached tags.</param>
/// <param name="CustomFields">Custom field values keyed by definition key.</param>
/// <param name="CreatedAt">Creation instant in UTC.</param>
/// <param name="UpdatedAt">Last change instant in UTC.</param>
/// <param name="Version">Row version to send back on update.</param>
/// <param name="OwnerDisplayName">Owner display name, or null when unowned, unnamed or outside the organization.</param>
/// <param name="OwnerEmail">Owner email, or null under the same conditions.</param>
/// <param name="CompanyName">Company name, or null when unset or when the caller cannot read that company.</param>
public sealed record ContactView(
    Guid Id,
    string FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Country,
    string? City,
    Guid? CompanyId,
    Guid? OwnerUserId,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyDictionary<string, JsonElement> CustomFields,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version,
    string? OwnerDisplayName,
    string? OwnerEmail,
    string? CompanyName);
