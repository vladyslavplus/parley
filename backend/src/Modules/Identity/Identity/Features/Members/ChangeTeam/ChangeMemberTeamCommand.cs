using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Members.ChangeTeam;

/// <summary>
/// Moves a member to another team, or out of any team when <paramref name="TeamId"/> is null.
/// </summary>
/// <param name="MemberId">Membership identifier in the current organization.</param>
/// <param name="TeamId">Target team of the current organization, or null for no team.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record ChangeMemberTeamCommand(Guid MemberId, Guid? TeamId) : IRequest<Result>;
