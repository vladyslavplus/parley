using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Infrastructure.Auth;

/// <summary>
/// Echoes the principal produced by the real JWT bearer scheme, proving that an
/// access token issued by sign-in is accepted and carries the subject and organization.
/// </summary>
[ApiController]
[Route(TestOnlyRoutes.CurrentPrincipal)]
public sealed class CurrentPrincipalTestController : ControllerBase
{
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult Get() => Ok(new CurrentPrincipalResponse(
        User.FindFirst(TenancyClaimTypes.UserId)?.Value,
        User.FindFirst(TenancyClaimTypes.OrganizationId)?.Value));
}
