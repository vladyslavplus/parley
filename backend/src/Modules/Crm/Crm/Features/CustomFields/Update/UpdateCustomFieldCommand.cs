using Mediator;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.CustomFields.Update;

/// <summary>
/// Changes the label, options, requirement and position of a definition. The key and the field
/// type never change. Archived definitions cannot be changed. Changing options does not rewrite
/// stored values; values outside the new options fail validation on the next write.
/// </summary>
[RequiresPermission(Permissions.CustomFieldsManage)]
public sealed record UpdateCustomFieldCommand(
    Guid Id,
    string Label,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int Position) : IRequest<Result<CustomFieldDefinitionView>>;
