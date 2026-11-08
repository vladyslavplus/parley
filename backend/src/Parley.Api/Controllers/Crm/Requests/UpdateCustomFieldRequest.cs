namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of custom field definition update. The key and the field type never change.</summary>
public sealed record UpdateCustomFieldRequest(
    string? Label,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int Position);
