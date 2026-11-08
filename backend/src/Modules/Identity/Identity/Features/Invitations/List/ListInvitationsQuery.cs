using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Invitations.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Invitations.List;

/// <summary>
/// Lists the pending invitations of the current organization: not accepted, not revoked and not expired.
/// Keyset-paged, newest first unless another sort is asked.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Q">Text the invited email must contain.</param>
/// <param name="RoleCode">Only invitations for this role code.</param>
/// <param name="Sort">Sort field: <c>createdAt</c> (default), <c>email</c> or <c>expiresAt</c>.</param>
/// <param name="Order">Sort direction: <c>desc</c> by default for <c>createdAt</c>, otherwise <c>asc</c>.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record ListInvitationsQuery(
    string? After,
    int? Limit,
    string? Q,
    string? RoleCode,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<InvitationView>>>;
