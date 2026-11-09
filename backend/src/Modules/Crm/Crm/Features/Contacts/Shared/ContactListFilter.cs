namespace Parley.Crm.Features.Contacts.Shared;

/// <summary>
/// The filters shared by the contact list and the contact export. Serialized as the export's
/// filter snapshot, so the background job applies the same predicates the request saw.
/// </summary>
/// <param name="Search">Case-insensitive text matched against the name and the email.</param>
/// <param name="TagId">Only contacts with this tag.</param>
/// <param name="CompanyId">Only contacts of this company.</param>
/// <param name="OwnerUserId">Only contacts owned by this user.</param>
/// <param name="CreatedFrom">First creation day, inclusive, UTC.</param>
/// <param name="CreatedTo">Last creation day, inclusive, UTC.</param>
public sealed record ContactListFilter(
    string? Search,
    Guid? TagId,
    Guid? CompanyId,
    Guid? OwnerUserId,
    DateOnly? CreatedFrom,
    DateOnly? CreatedTo);
