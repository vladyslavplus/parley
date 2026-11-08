namespace Parley.Crm.Features.Search.Shared;

/// <summary>Contact columns read by the search, with the name similarity score.</summary>
public sealed record ContactSearchRow(Guid Id, string FirstName, string? LastName, string? Email, double Score);
