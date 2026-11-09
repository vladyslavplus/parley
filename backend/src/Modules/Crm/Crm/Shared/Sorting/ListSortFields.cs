namespace Parley.Crm.Shared.Sorting;

/// <summary>Sort fields accepted by the contact and company lists. Ties are broken by identifier.</summary>
public static class ListSortFields
{
    /// <summary>Full name for contacts, name for companies. Default for both lists.</summary>
    public const string Name = "name";

    /// <summary>Creation instant.</summary>
    public const string Created = "created";

    public static IReadOnlyList<string> Values { get; } = [Name, Created];

    public static bool IsValid(string? value) =>
        value is null or Name or Created;
}
