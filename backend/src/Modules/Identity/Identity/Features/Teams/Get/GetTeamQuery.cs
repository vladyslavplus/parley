using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Teams.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Teams.Get;

/// <summary>Reads one team of the current organization. Other organizations' teams are not found.</summary>
/// <param name="TeamId">Team identifier.</param>
[RequiresPermission(Permissions.TeamsManage)]
public sealed record GetTeamQuery(Guid TeamId) : IRequest<Result<TeamView>>;
