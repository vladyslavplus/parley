namespace Parley.Crm.Contracts.Search;

/// <summary>
/// One global search hit. <paramref name="Kind"/> is a <see cref="SearchResultKinds"/> value,
/// <paramref name="Title"/> is the display name and <paramref name="Subtitle"/> a secondary
/// line such as the email or the domain. Carries no highlight markup.
/// </summary>
public sealed record SearchResultItem(string Kind, Guid Id, string Title, string? Subtitle);
