using Mediator;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.CustomFields.List;

/// <summary>
/// Lists custom field definitions, archived ones included, keyset-paged. Sorted by label unless another sort is asked.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Q">Text the label or the key must contain.</param>
/// <param name="EntityType">Only definitions of this record type.</param>
/// <param name="FieldType">Only definitions of this field type.</param>
/// <param name="Sort">Sort field: <c>name</c> (default), <c>createdAt</c> or <c>position</c>.</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.ContactsRead)]
public sealed record ListCustomFieldsQuery(
    string? After,
    int? Limit,
    string? Q,
    string? EntityType,
    string? FieldType,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<CustomFieldDefinitionView>>>;
