using Mediator;
using Parley.Crm.Features.Contacts.Exports.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Exports.Get;

/// <summary>Status of one export. Only the user who requested it can read it; others get not found.</summary>
[RequiresPermission(Permissions.ContactsExport)]
public sealed record GetContactExportQuery(Guid Id) : IRequest<Result<ContactExportView>>;
