using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Teams.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Teams.Update;

/// <summary>Renames a team of the current organization.</summary>
/// <param name="TeamId">Team identifier.</param>
/// <param name="Name">Team name to set.</param>
[RequiresPermission(Permissions.TeamsManage)]
public sealed record UpdateTeamCommand(Guid TeamId, string Name) : IRequest<Result<TeamView>>;
