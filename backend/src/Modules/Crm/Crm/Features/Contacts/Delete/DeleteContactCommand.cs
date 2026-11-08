using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Delete;

/// <summary>Soft deletes one contact. Emits <c>ContactDeleted</c> and an audit record in the same transaction.</summary>
/// <param name="Id">Contact identifier.</param>
[RequiresPermission(Permissions.ContactsDelete)]
public sealed record DeleteContactCommand(Guid Id) : IRequest<Result>;
