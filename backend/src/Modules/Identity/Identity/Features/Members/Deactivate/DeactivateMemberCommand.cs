using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Members.Deactivate;

/// <summary>
/// Deactivates a member. The member loses access on the next request, and cannot sign in
/// or refresh. Administrators cannot deactivate themselves, and the last active administrator
/// cannot be deactivated.
/// </summary>
/// <param name="MemberId">Membership identifier in the current organization.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record DeactivateMemberCommand(Guid MemberId) : IRequest<Result>;
