using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Members.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Members.List;

/// <summary>
/// Lists the members of the current organization, keyset-paged. Shows active members unless another status is asked.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Q">Text the display name or the email must contain.</param>
/// <param name="RoleCode">Only members with this role code.</param>
/// <param name="TeamId">Only members of this team, as GUID text.</param>
/// <param name="Status">Membership status: <c>active</c> (default), <c>deactivated</c> or <c>all</c>.</param>
/// <param name="Sort">Sort field: <c>name</c> (default), <c>email</c>, <c>role</c> or <c>joinedAt</c>.</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record ListMembersQuery(
    string? After,
    int? Limit,
    string? Q,
    string? RoleCode,
    string? TeamId,
    string? Status,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<MemberView>>>;
