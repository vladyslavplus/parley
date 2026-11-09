using Mediator;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Get;

/// <summary>Reads one contact the caller may see. Out-of-scope and deleted contacts return not found.</summary>
/// <param name="Id">Contact identifier.</param>
[RequiresPermission(Permissions.ContactsRead)]
public sealed record GetContactQuery(Guid Id) : IRequest<Result<ContactView>>;
