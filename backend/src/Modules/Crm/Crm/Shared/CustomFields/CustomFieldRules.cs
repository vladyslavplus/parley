using Parley.Crm.Domain.CustomFields;

namespace Parley.Crm.Shared.CustomFields;

/// <summary>Rules shared by custom field definition validation and value validation.</summary>
public static class CustomFieldRules
{
    /// <summary>Keys are lowercase identifiers so they can be used as JSON keys and in filters.</summary>
    public const string KeyPattern = "^[a-z][a-z0-9_]*$";

    /// <summary>Calendar date format of date values.</summary>
    public const string DateFormat = "yyyy-MM-dd";

    public static IReadOnlyList<string> EntityTypes { get; } =
    [
        CustomFieldEntityTypes.Contact,
        CustomFieldEntityTypes.Company,
    ];

    public static IReadOnlyList<string> FieldTypes { get; } =
    [
        CustomFieldTypes.Text,
        CustomFieldTypes.Number,
        CustomFieldTypes.Date,
        CustomFieldTypes.Boolean,
        CustomFieldTypes.Select,
    ];

    public static bool IsValidEntityType(string? value) => value is not null && EntityTypes.Contains(value);

    public static bool IsValidFieldType(string? value) => value is not null && FieldTypes.Contains(value);
}
