namespace Parley.Crm.Contracts.Recipients;

/// <summary>
/// The values a campaign email may print for one live contact. Names and the company name only; the address is not
/// part of this row, because the campaign recipient carries the address that was frozen with the audience.
/// </summary>
/// <param name="ContactId">The contact.</param>
/// <param name="FirstName">First name of the contact.</param>
/// <param name="LastName">Last name of the contact; null when unknown.</param>
/// <param name="CompanyName">Name of the contact's company; null when the contact has no company.</param>
/// <param name="IsDeleted">True when the contact was soft-deleted after the campaign froze its audience. The send path never mails it.</param>
public sealed record CrmRecipientProfileRow(Guid ContactId, string FirstName, string? LastName, string? CompanyName, bool IsDeleted);
