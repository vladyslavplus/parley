using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Teams.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Teams.List;

/// <summary>Lists the teams of the current organization, keyset-paged, by name unless another sort is asked.</summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Q">Text the team name must contain.</param>
/// <param name="Sort">Sort field: <c>name</c> (default) or <c>createdAt</c>.</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.TeamsManage)]
public sealed record ListTeamsQuery(
    string? After,
    int? Limit,
    string? Q,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<TeamView>>>;
