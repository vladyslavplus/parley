namespace Parley.SharedKernel.Paging;

/// <summary>Sort direction values accepted by list endpoints.</summary>
public static class SortDirections
{
    private const string Ascending = "asc";

    private const string Descending = "desc";

    public static IReadOnlyList<string> All { get; } = [Ascending, Descending];

    /// <summary>True when the value is absent or one of the direction values.</summary>
    public static bool IsValid(string? value) =>
        string.IsNullOrEmpty(value) || value is Ascending or Descending;

    /// <summary>Direction of a list: the requested one, or <paramref name="defaultDescending"/> when none is given.</summary>
    public static bool IsDescending(string? value, bool defaultDescending = false) =>
        string.IsNullOrEmpty(value) ? defaultDescending : value == Descending;
}
