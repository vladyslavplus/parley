using Mediator;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.CustomFields.List;

/// <summary>
/// Lists custom field definitions, archived ones included, ordered by position then key. The
/// list is not paged: an organization holds at most a few dozen definitions per record type.
/// </summary>
/// <param name="EntityType">Only definitions of this record type, or null for all record types.</param>
[RequiresPermission(Permissions.ContactsRead)]
public sealed record ListCustomFieldsQuery(string? EntityType) : IRequest<Result<IReadOnlyList<CustomFieldDefinitionView>>>;
