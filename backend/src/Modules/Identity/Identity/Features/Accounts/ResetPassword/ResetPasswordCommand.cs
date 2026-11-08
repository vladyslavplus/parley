using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.ResetPassword;

/// <summary>
/// Sets a new password with the token from the reset link. The token is single-use
/// (the password change rotates the security stamp), and every refresh token of the
/// account is revoked, so existing sessions end.
/// </summary>
/// <param name="UserId">Account identifier from the link.</param>
/// <param name="Token">Reset token from the link.</param>
/// <param name="NewPassword">The new password, hashed before storage, never logged.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword) : IRequest<Result>;
