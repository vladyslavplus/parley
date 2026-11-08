using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Invitations.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Invitations.List;

/// <summary>
/// Lists the pending invitations of the current organization: not accepted, not revoked
/// and not expired. Keyset-paged by identifier.
/// </summary>
/// <param name="After">Last identifier of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record ListInvitationsQuery(Guid? After, int? Limit) : IRequest<Result<CursorPage<InvitationView>>>;
