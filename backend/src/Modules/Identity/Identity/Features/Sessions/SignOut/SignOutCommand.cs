using Mediator;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Sessions.SignOut;

/// <summary>
/// Ends the session that owns the presented refresh token. Succeeds whether or not
/// the token is known, so the response reveals nothing about it.
/// </summary>
/// <param name="RefreshToken">Opaque refresh value of the session to end.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record SignOutCommand(string RefreshToken) : IRequest<Result>;
