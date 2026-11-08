using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Members.ChangeRole;

/// <summary>
/// Changes the system role of a member. The last active administrator cannot be demoted.
/// The membership is read per request, so the change applies without invalidating a cache.
/// </summary>
/// <param name="MemberId">Membership identifier in the current organization.</param>
/// <param name="RoleCode">Code of the new system role.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record ChangeMemberRoleCommand(Guid MemberId, string RoleCode) : IRequest<Result>;
