using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Identity.Requests;
using Parley.Api.Localization;
using Parley.Identity.Features.Organizations.ListMine;
using Parley.Identity.Features.Profile.GetCurrentUser;
using Parley.Identity.Features.Profile.UpdateProfile;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Identity;

/// <summary>
/// Own profile and the signed-in view: <c>GET /me</c> for the interface gating data,
/// <c>GET /me/organizations</c> for the organizations the user belongs to, and
/// <c>PUT /me/profile</c> for the personal settings.
/// </summary>
public sealed class MeController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(IdentityRoutes.Me)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<CurrentUserView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(IdentityRoutes.MeOrganizations)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<IReadOnlyList<MyOrganizationView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListOrganizations(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListMyOrganizationsQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(IdentityRoutes.MeProfile)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateProfileCommand(request?.DisplayName, request?.Locale ?? string.Empty, request?.TimeZone ?? string.Empty, request?.Theme ?? string.Empty),
            cancellationToken);
        return FromResult(result);
    }
}
