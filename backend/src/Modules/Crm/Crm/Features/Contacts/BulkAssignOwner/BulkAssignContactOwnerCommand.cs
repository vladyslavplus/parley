using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.BulkAssignOwner;

/// <summary>
/// Assigns one owner to several contacts. All-or-nothing: if any listed contact is missing or
/// outside the caller's scope, nothing changes and the answer is not found.
/// </summary>
/// <param name="ContactIds">Contacts to change.</param>
/// <param name="OwnerUserId">New owner, which must lie inside the caller's scope.</param>
[RequiresPermission(Permissions.ContactsWrite)]
public sealed record BulkAssignContactOwnerCommand(IReadOnlyList<Guid> ContactIds, Guid OwnerUserId) : IRequest<Result>;
