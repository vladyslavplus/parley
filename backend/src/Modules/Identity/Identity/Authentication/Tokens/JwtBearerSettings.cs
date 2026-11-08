using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Parley.Identity.Options.Auth;

namespace Parley.Identity.Authentication.Tokens;

/// <summary>
/// Validation rules for the access tokens issued by <see cref="AccessTokenIssuer"/>:
/// signature, issuer, audience and lifetime. Inbound claim mapping is off, so the claim
/// types written by the issuer are read back unchanged.
/// </summary>
public static class JwtBearerSettings
{
    /// <summary>Tolerance for clock drift between the issuing and validating hosts.</summary>
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static void Apply(JwtBearerOptions options, AuthOptions auth)
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = auth.Issuer,
            ValidateAudience = true,
            ValidAudience = auth.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = ClockSkew,
        };
    }
}
