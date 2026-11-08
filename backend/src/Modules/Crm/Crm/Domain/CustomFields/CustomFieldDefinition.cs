using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.CustomFields;

/// <summary>
/// Definition of a custom field of one record type. Archiving hides the field and keeps the
/// stored values; the key never changes after creation.
/// </summary>
public sealed class CustomFieldDefinition : BusinessEntity
{
    public string EntityType { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    /// <summary>One of the values in <see cref="CustomFieldTypes"/>.</summary>
    public string FieldType { get; set; } = string.Empty;

    /// <summary>Allowed values of a select field as a JSON array of strings.</summary>
    public string Options { get; set; } = CrmLimits.EmptyJsonArray;

    public bool IsRequired { get; set; }

    public int Position { get; set; }

    public DateTime? ArchivedAt { get; set; }
}
