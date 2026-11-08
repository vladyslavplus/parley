using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Infrastructure.Auth;

/// <summary>
/// Test-only authentication handler. Claims carry the organization from
/// <see cref="TenancyClaimTypes.OrganizationId"/> and the user from the
/// standard name identifier claim.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var organizationClaim = Request.Headers.TryGetValue(TestAuthDefaults.OrganizationHeader, out var organizationHeader)
            ? organizationHeader.ToString()
            : string.Empty;
        var userClaim = Request.Headers.TryGetValue(TestAuthDefaults.UserHeader, out var userHeader)
            ? userHeader.ToString()
            : string.Empty;

        if (!Guid.TryParse(organizationClaim, out var organizationId) || !Guid.TryParse(userClaim, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
        [
            new Claim(TenancyClaimTypes.OrganizationId, organizationId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        ], TestAuthDefaults.AuthenticationScheme);
        if (Request.Headers.TryGetValue(TestAuthDefaults.PlatformOperatorHeader, out var operatorHeader)
            && string.Equals(operatorHeader.ToString(), TenancyClaimTypes.PlatformOperatorValue, StringComparison.OrdinalIgnoreCase))
        {
            identity.AddClaim(new Claim(TenancyClaimTypes.PlatformOperator, TenancyClaimTypes.PlatformOperatorValue));
        }

        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, TestAuthDefaults.AuthenticationScheme)));
    }
}
