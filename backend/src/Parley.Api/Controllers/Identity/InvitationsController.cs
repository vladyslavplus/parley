using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Identity.Requests;
using Parley.Api.Localization;
using Parley.Api.RateLimiting;
using Parley.Identity.Authentication.Tokens;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Invitations.Accept;
using Parley.Identity.Features.Invitations.Invite;
using Parley.Identity.Features.Invitations.List;
using Parley.Identity.Features.Invitations.Revoke;
using Parley.Identity.Features.Invitations.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Identity;

/// <summary>Invitations: create, list pending, revoke, and accept with the emailed token.</summary>
public sealed class InvitationsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(IdentityRoutes.Invitations)]
    [RequiresPermission(Permissions.UsersManage)]
    [ProducesResponseType<CursorPage<InvitationView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([FromQuery] Guid? after, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListInvitationsQuery(after, limit), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(IdentityRoutes.Invitations)]
    [RequiresPermission(Permissions.UsersManage)]
    [EnableRateLimiting(AuthRateLimitPolicies.InvitationCreate)]
    [ProducesResponseType<InvitationView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Invite([FromBody] InviteRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new InviteMemberCommand(
                request?.Email ?? string.Empty,
                request?.RoleCode ?? string.Empty,
                request?.TeamId,
                RequestCulture.TwoLetter()),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(IdentityRoutes.InvitationRevoke)]
    [RequiresPermission(Permissions.UsersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RevokeInvitationCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost(IdentityRoutes.InvitationAccept)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(AuthRateLimitPolicies.InvitationAccept)]
    [ProducesResponseType<AuthTokens>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Accept([FromBody] AcceptInvitationRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AcceptInvitationCommand(
                request?.Token ?? string.Empty,
                request?.Password ?? string.Empty,
                RequestCulture.TwoLetter()),
            cancellationToken);
        return FromResult(result, Ok);
    }
}
