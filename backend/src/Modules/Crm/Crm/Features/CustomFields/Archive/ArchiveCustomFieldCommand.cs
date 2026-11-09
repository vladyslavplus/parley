using Mediator;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.CustomFields.Archive;

/// <summary>
/// Archives a definition. The field disappears from forms and validation; stored values stay on
/// the records. Archiving an archived definition succeeds without change.
/// </summary>
/// <param name="Id">Definition identifier.</param>
[RequiresPermission(Permissions.CustomFieldsManage)]
public sealed record ArchiveCustomFieldCommand(Guid Id) : IRequest<Result<CustomFieldDefinitionView>>;
