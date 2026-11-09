namespace Parley.Crm.Contracts.Audience;

/// <summary>
/// A live contact that has an email address. Identifiers and the country only; the address is read on the send path.
/// </summary>
/// <param name="ContactId">The contact.</param>
/// <param name="OwnerUserId">Owner of the contact; null when unowned.</param>
/// <param name="CountryIso2">ISO 3166-1 alpha-2 code, or null when unknown.</param>
public sealed record CrmAudienceRow(Guid ContactId, Guid? OwnerUserId, string? CountryIso2);
