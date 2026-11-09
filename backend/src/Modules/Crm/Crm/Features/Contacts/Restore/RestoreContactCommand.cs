using Mediator;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Restore;

/// <summary>
/// Restores a soft-deleted contact. Fails with 409 when another live contact now uses the same email.
/// </summary>
/// <param name="Id">Contact identifier.</param>
[RequiresPermission(Permissions.ContactsDelete)]
public sealed record RestoreContactCommand(Guid Id) : IRequest<Result<ContactView>>;
