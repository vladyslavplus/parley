namespace Parley.Crm.Features.Companies.Shared;

/// <summary>Raw company columns read in one query. Mapped to <see cref="CompanyView"/> in memory.</summary>
public sealed record CompanyRow(
    Guid Id,
    string Name,
    string? Domain,
    string? Country,
    string? Industry,
    Guid? OwnerUserId,
    string CustomFields,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    uint Version,
    string NameNormalized);
