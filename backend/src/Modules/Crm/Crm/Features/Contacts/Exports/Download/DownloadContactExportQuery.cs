using Mediator;
using Parley.Crm.Features.Contacts.Exports.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Exports.Download;

/// <summary>
/// Resolves the file of a completed export for its requester. Fails with not found for another
/// user's export, conflict while the file is not ready, and not found with
/// <c>crm.export.expired</c> once the link has expired.
/// </summary>
[RequiresPermission(Permissions.ContactsExport)]
public sealed record DownloadContactExportQuery(Guid Id) : IRequest<Result<ContactExportFile>>;
