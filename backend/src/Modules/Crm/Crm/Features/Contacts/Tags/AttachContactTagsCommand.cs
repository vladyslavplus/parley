using Mediator;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Tags;

/// <summary>Attaches tags to a contact. Tags already attached stay attached; unknown tags return not found.</summary>
/// <param name="ContactId">Contact identifier.</param>
/// <param name="TagIds">Tags to attach.</param>
[RequiresPermission(Permissions.ContactsWrite)]
public sealed record AttachContactTagsCommand(Guid ContactId, IReadOnlyList<Guid> TagIds) : IRequest<Result<ContactView>>;
