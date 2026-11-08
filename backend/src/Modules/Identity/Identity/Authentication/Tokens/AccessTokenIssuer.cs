using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Parley.Identity.Options.Auth;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Authentication.Tokens;

/// <summary>
/// Issues the signed JWT access tokens. The token carries the subject, the active
/// organization (<see cref="TenancyClaimTypes.OrganizationId"/>) and the user identifier
/// (<see cref="ClaimTypes.NameIdentifier"/> and <see cref="TenancyClaimTypes.UserId"/>).
/// Times come from <see cref="TimeProvider"/> so tests control
/// the issue and expiry instants.
/// </summary>
public sealed class AccessTokenIssuer(IOptions<AuthOptions> options, TimeProvider timeProvider)
{
    private readonly AuthOptions _options = options.Value;

    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.SigningKey)),
        SecurityAlgorithms.HmacSha256);

    private readonly JsonWebTokenHandler _handler = new();

    /// <summary>
    /// Creates a token for <paramref name="userId"/> acting in <paramref name="organizationId"/>.
    /// The platform-operator claim is added only for operators.
    /// </summary>
    /// <returns>The serialized token and its UTC expiry.</returns>
    public (string Token, DateTimeOffset ExpiresAt) Issue(Guid userId, Guid organizationId, bool isPlatformOperator)
    {
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.Add(_options.AccessTokenLifetime);

        var claims = new List<Claim>(capacity: 4)
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(TenancyClaimTypes.OrganizationId, organizationId.ToString()),
            new(TenancyClaimTypes.UserId, userId.ToString()),
        };
        if (isPlatformOperator)
        {
            claims.Add(new Claim(TenancyClaimTypes.PlatformOperator, TenancyClaimTypes.PlatformOperatorValue));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _credentials,
        };

        return (_handler.CreateToken(descriptor), expiresAt);
    }
}
