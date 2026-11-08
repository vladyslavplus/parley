namespace Parley.Identity.Features.Teams.Shared;

/// <summary>Sort fields of the team list.</summary>
public static class TeamListRules
{
    public const string SortName = "name";

    public const string SortCreatedAt = "createdAt";

    public static IReadOnlyList<string> SortFields { get; } = [SortName, SortCreatedAt];

    /// <summary>True when the value is absent or one of the sort fields.</summary>
    public static bool IsValidSort(string? value) =>
        string.IsNullOrEmpty(value) || value is SortName or SortCreatedAt;
}
