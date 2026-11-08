namespace Parley.Identity.Authentication.Tokens;

/// <summary>
/// A short-lived JWT access token with its rotating refresh token. The refresh
/// value is opaque and is returned once; the server keeps only its hash.
/// </summary>
/// <param name="AccessToken">Signed JWT, sent as a bearer token.</param>
/// <param name="AccessTokenExpiresAt">UTC expiry of <paramref name="AccessToken"/>.</param>
/// <param name="RefreshToken">Opaque refresh value.</param>
/// <param name="RefreshTokenExpiresAt">UTC expiry of <paramref name="RefreshToken"/>.</param>
public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
