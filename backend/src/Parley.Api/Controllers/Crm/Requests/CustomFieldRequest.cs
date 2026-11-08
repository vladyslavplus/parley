namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of custom field definition create.</summary>
public sealed record CustomFieldRequest(
    string? EntityType,
    string? Key,
    string? Label,
    string? FieldType,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int Position);
