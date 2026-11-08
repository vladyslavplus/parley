using Mediator;
using Parley.Identity.Authentication.Refresh;
using Parley.Identity.Authentication.Tokens;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Identity.Features.Sessions.Refresh;

/// <summary>
/// Rotates a refresh token and returns a new access and refresh pair. The rules
/// (grace window, family revocation) live in <see cref="RefreshTokenService"/>.
/// </summary>
/// <param name="RefreshToken">Opaque refresh value from the last pair.</param>
[PermissionOptOut(PermissionOptOutKind.Anonymous)]
public sealed record RefreshSessionCommand(string RefreshToken) : IRequest<Result<AuthTokens>>;
