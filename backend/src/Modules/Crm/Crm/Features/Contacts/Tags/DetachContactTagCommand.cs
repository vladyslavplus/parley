using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Tags;

/// <summary>Detaches one tag from a contact. Detaching a tag that is not attached succeeds.</summary>
/// <param name="ContactId">Contact identifier.</param>
/// <param name="TagId">Tag identifier.</param>
[RequiresPermission(Permissions.ContactsWrite)]
public sealed record DetachContactTagCommand(Guid ContactId, Guid TagId) : IRequest<Result>;
