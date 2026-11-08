using Mediator;
using Parley.Identity.Authentication.Tokens;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Invitations.Accept;

/// <summary>
/// Accepts an invitation with its emailed token and signs the person in to the invited
/// organization. The token is the credential, so the request is anonymous. The password
/// either creates the account (address not yet registered) or proves the existing account.
/// The invitation moves from pending to accepted in one conditional update, so it works once.
/// </summary>
/// <param name="Token">Invitation token from the link.</param>
/// <param name="Password">Password of the new account, or of the existing account for the same address.</param>
/// <param name="Culture">Request culture, used as the locale of a new account.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record AcceptInvitationCommand(string Token, string Password, string Culture) : IRequest<Result<AuthTokens>>;
