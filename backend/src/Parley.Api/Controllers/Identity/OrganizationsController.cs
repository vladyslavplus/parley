using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Auth.Requests;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Identity.Requests;
using Parley.Api.Localization;
using Parley.Identity.Authentication.Tokens;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Organizations.Get;
using Parley.Identity.Features.Organizations.Switch;
using Parley.Identity.Features.Organizations.Update;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Identity;

/// <summary>Organization settings and switching between organizations.</summary>
public sealed class OrganizationsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(IdentityRoutes.OrganizationById)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<OrganizationView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOrganizationQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(IdentityRoutes.OrganizationById)]
    [RequiresPermission(Permissions.OrgManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrganizationRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateOrganizationCommand(
                id,
                request?.Name ?? string.Empty,
                request?.TimeZone ?? string.Empty,
                request?.DefaultLocale ?? string.Empty,
                request?.Currency ?? string.Empty),
            cancellationToken);
        return FromResult(result);
    }

    [HttpPost(IdentityRoutes.OrganizationSwitch)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Switch(Guid id, [FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SwitchOrganizationCommand(id, request?.RefreshToken ?? string.Empty), cancellationToken);
        return FromResult(result, Ok);
    }
}
