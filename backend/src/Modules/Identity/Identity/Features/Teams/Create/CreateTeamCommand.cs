using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Teams.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Teams.Create;

/// <summary>Creates a team. Names are unique per organization, ignoring letter case.</summary>
/// <param name="Name">Team name.</param>
[RequiresPermission(Permissions.TeamsManage)]
public sealed record CreateTeamCommand(string Name) : IRequest<Result<TeamView>>;
