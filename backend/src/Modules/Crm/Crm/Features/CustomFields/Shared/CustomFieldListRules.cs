namespace Parley.Crm.Features.CustomFields.Shared;

/// <summary>Sort fields of the custom field definition list.</summary>
public static class CustomFieldListRules
{
    /// <summary>Label, the name a user sees. Default sort.</summary>
    public const string SortName = "name";

    /// <summary>Creation instant.</summary>
    public const string SortCreatedAt = "createdAt";

    /// <summary>Position on the record form.</summary>
    public const string SortPosition = "position";

    public static IReadOnlyList<string> SortFields { get; } = [SortName, SortCreatedAt, SortPosition];

    /// <summary>True when the value is absent or one of the sort fields.</summary>
    public static bool IsValidSort(string? value) =>
        string.IsNullOrEmpty(value) || value is SortName or SortCreatedAt or SortPosition;
}
