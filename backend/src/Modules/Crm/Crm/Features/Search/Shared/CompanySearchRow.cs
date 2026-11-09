namespace Parley.Crm.Features.Search.Shared;

/// <summary>Company columns read by the search, with the name similarity score.</summary>
public sealed record CompanySearchRow(Guid Id, string Name, string? Domain, double Score);
