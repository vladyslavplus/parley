using Mediator;
using Parley.Crm.Features.Contacts.Exports.Shared;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Exports.Request;

/// <summary>
/// Requests a CSV export of the contacts the caller may see under the given filter. Creates the
/// export row and its audit event in one transaction, then queues the file job. The API replays
/// the stored response for a repeated <c>Idempotency-Key</c>, so no second export is queued.
/// </summary>
[RequiresPermission(Permissions.ContactsExport)]
public sealed record RequestContactExportCommand(ContactListFilter Filter) : IRequest<Result<ContactExportView>>;
