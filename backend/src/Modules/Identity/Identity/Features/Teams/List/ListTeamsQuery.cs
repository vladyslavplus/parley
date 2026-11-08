using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Teams.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Teams.List;

/// <summary>Lists the teams of the current organization, keyset-paged by identifier.</summary>
/// <param name="After">Last identifier of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
[RequiresPermission(Permissions.TeamsManage)]
public sealed record ListTeamsQuery(Guid? After, int? Limit) : IRequest<Result<CursorPage<TeamView>>>;
