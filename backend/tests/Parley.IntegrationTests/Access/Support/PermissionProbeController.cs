using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Identity.Authorization.Behavior;
using Parley.SharedKernel.Results;

namespace Parley.IntegrationTests.Access.Support;

/// <summary>
/// Runs the real permission behavior for <see cref="PermissionProbeRequest"/> on an
/// authenticated HTTP request, so the bearer token, tenant claim, membership, role
/// grant and the shared ProblemDetails mapping are all exercised end to end.
/// </summary>
[ApiController]
[Route(PermissionProbeRoutes.Probe)]
public sealed class PermissionProbeController(
    PermissionAuthorizationBehavior<PermissionProbeRequest, Result> authorization,
    ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await authorization.Handle(
            new PermissionProbeRequest(),
            (_, _) => ValueTask.FromResult(Result.Success()),
            cancellationToken);
        return FromResult(result);
    }
}
