namespace Parley.Crm.Features.Contacts.Shared;

/// <summary>
/// Raw contact columns read in one query, including the tag identifiers. Mapped to
/// <see cref="ContactView"/> in memory, because stored custom fields are parsed there.
/// </summary>
public sealed record ContactRow(
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
    string CustomFields,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version,
    IReadOnlyList<Guid> TagIds,
    string NameNormalized,
    string? CompanyName);
