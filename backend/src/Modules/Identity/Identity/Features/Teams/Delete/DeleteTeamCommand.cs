using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Teams.Delete;

/// <summary>
/// Deletes a team. Members of the team stay in the organization without a team.
/// </summary>
/// <param name="TeamId">Team identifier.</param>
[RequiresPermission(Permissions.TeamsManage)]
public sealed record DeleteTeamCommand(Guid TeamId) : IRequest<Result>;
