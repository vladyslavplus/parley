namespace Parley.Crm.Contracts.Search;

/// <summary>Global search result, best match first, capped by the search result limit.</summary>
public sealed record SearchResponse(IReadOnlyList<SearchResultItem> Items);
