using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.BulkDelete;

/// <summary>
/// Soft deletes several contacts. All-or-nothing: if any listed contact is missing or outside
/// the caller's scope, nothing changes and the answer is not found.
/// </summary>
/// <param name="ContactIds">Contacts to delete.</param>
[RequiresPermission(Permissions.ContactsDelete)]
public sealed record BulkDeleteContactsCommand(IReadOnlyList<Guid> ContactIds) : IRequest<Result>;
