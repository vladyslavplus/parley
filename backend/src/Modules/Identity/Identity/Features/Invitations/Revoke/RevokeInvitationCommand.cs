using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Invitations.Revoke;

/// <summary>
/// Revokes a pending invitation so its link stops working. An invitation that was already
/// accepted or revoked cannot be revoked again.
/// </summary>
/// <param name="InvitationId">Invitation of the current organization.</param>
[RequiresPermission(Permissions.UsersManage)]
public sealed record RevokeInvitationCommand(Guid InvitationId) : IRequest<Result>;
