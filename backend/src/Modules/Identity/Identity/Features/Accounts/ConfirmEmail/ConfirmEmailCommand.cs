using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Accounts.ConfirmEmail;

/// <summary>
/// Confirms the email address with the token from the confirmation link. The
/// token is single-use: confirming changes the security stamp, which the token
/// embeds, so a second use fails.
/// </summary>
/// <param name="UserId">Account identifier from the link.</param>
/// <param name="Token">Confirmation token from the link.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record ConfirmEmailCommand(Guid UserId, string Token) : IRequest<Result>;
