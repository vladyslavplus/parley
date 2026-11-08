namespace Parley.Crm.Contracts.Search;

/// <summary>
/// Kinds of records returned by the global search. Deals join this list when the deal
/// module lands; clients branch on the string and ignore kinds they do not know.
/// </summary>
public static class SearchResultKinds
{
    public const string Contact = "contact";

    public const string Company = "company";

    public static IReadOnlyList<string> All { get; } = [Contact, Company];
}
