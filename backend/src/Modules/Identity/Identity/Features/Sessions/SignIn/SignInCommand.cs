using Mediator;
using Parley.Identity.Authentication.Tokens;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Sessions.SignIn;

/// <summary>
/// Signs in with email and password and starts a new session family.
/// </summary>
/// <remarks>
/// <para>
/// Unknown addresses, wrong passwords and locked accounts all return the same
/// invalid-credentials error. A locked account is not revealed, because the lockout
/// only exists for real accounts and would otherwise show which addresses exist.
/// </para>
/// <para>
/// The unconfirmed-email refusal is returned only after the password was correct, so
/// it discloses nothing to someone who does not know the password.
/// </para>
/// </remarks>
/// <param name="Email">Sign-in address.</param>
/// <param name="Password">Password, verified and never logged.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record SignInCommand(string Email, string Password) : IRequest<Result<AuthTokens>>;
