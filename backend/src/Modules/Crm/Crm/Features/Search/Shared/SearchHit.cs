namespace Parley.Crm.Features.Search.Shared;

/// <summary>One ranked candidate before the merge; mapped to <c>SearchResultItem</c> at the end.</summary>
public sealed record SearchHit(string Kind, Guid Id, string Title, string? Subtitle, double Score);
