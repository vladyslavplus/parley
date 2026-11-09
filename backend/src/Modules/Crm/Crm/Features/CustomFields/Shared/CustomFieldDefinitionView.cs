namespace Parley.Crm.Features.CustomFields.Shared;

/// <summary>A custom field definition as the API returns it.</summary>
/// <param name="Id">Definition identifier.</param>
/// <param name="EntityType">Record type the field belongs to: <c>contact</c> or <c>company</c>.</param>
/// <param name="Key">Stable key used in the record's custom field object. Never changes.</param>
/// <param name="Label">Display label.</param>
/// <param name="FieldType">Value type: <c>text</c>, <c>number</c>, <c>date</c>, <c>boolean</c> or <c>select</c>. Never changes.</param>
/// <param name="Options">Allowed values of a select field; empty for other types.</param>
/// <param name="IsRequired">Whether every record must carry a value.</param>
/// <param name="Position">Display order; lower values come first.</param>
/// <param name="ArchivedAt">When the field was archived, or null while it is active.</param>
public sealed record CustomFieldDefinitionView(
    Guid Id,
    string EntityType,
    string Key,
    string Label,
    string FieldType,
    IReadOnlyList<string> Options,
    bool IsRequired,
    int Position,
    DateTime? ArchivedAt);
