namespace Parley.Crm.Shared.Sorting;

/// <summary>Sort direction values accepted by list endpoints.</summary>
public static class SortDirections
{
    private const string Ascending = "asc";

    private const string Descending = "desc";

    public static bool IsValid(string? value) =>
        value is null or Ascending or Descending;

    public static bool IsDescending(string? value) => value == Descending;
}
