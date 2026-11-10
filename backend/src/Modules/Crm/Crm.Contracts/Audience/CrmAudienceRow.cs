namespace Parley.Crm.Contracts.Audience;

/// <summary>
/// A live contact that has an email address. Identifiers, the country, tags, the last order instant and the address
/// itself. The address is taken for the campaigns audience freeze snapshot and suppression match, and is not logged.
/// </summary>
/// <param name="ContactId">The contact.</param>
/// <param name="OwnerUserId">Owner of the contact; null when unowned.</param>
/// <param name="CountryIso2">ISO 3166-1 alpha-2 code, or null when unknown.</param>
/// <param name="TagIds">Tags attached to the contact; null when the row was built without tag data, read as no tags.</param>
/// <param name="LastOrderAt">Last order of the contact, UTC; null without orders.</param>
/// <param name="Email">The address as stored.</param>
/// <param name="EmailNormalized">The address in its normalized (lowercase) form, the form suppressions match on.</param>
public sealed record CrmAudienceRow(
    Guid ContactId,
    Guid? OwnerUserId,
    string? CountryIso2,
    IReadOnlyList<Guid>? TagIds = null,
    DateTime? LastOrderAt = null,
    string? Email = null,
    string? EmailNormalized = null);
