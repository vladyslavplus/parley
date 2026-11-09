using Mediator;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.CustomFields.Create;

/// <summary>Creates a custom field definition. Select fields need at least one option; other types take none.</summary>
/// <param name="EntityType">Record type: <c>contact</c> or <c>company</c>.</param>
/// <param name="Key">Stable lowercase key, unique per record type.</param>
/// <param name="Label">Display label.</param>
/// <param name="FieldType">Value type.</param>
/// <param name="Options">Allowed values, for select fields only.</param>
/// <param name="IsRequired">Whether every record must carry a value.</param>
/// <param name="Position">Display order.</param>
[RequiresPermission(Permissions.CustomFieldsManage)]
public sealed record CreateCustomFieldCommand(
    string EntityType,
    string Key,
    string Label,
    string FieldType,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int Position) : IRequest<Result<CustomFieldDefinitionView>>;
