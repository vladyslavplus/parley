namespace Parley.Crm.Shared.Deals;

/// <summary>Sort fields accepted by the deal list. Ties are broken by identifier.</summary>
public static class DealSortFields
{
    /// <summary>Title, case-insensitive. Default.</summary>
    public const string Name = "name";

    /// <summary>Amount.</summary>
    public const string Amount = "amount";

    /// <summary>Creation instant.</summary>
    public const string Created = "created";

    /// <summary>Board order inside the stage; the sort of a board column.</summary>
    public const string Position = "position";

    public static IReadOnlyList<string> Values { get; } = [Name, Amount, Created, Position];

    public static bool IsValid(string? value) =>
        value is null || Values.Contains(value);
}
