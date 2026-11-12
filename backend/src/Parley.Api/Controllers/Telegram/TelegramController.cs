using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Notifications.Features.Telegram.Link;
using Parley.Notifications.Features.Telegram.LinkCode;
using Parley.Notifications.Features.Telegram.Shared;
using Parley.Notifications.Features.Telegram.Status;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Telegram;

/// <summary>
/// The signed-in member's Telegram link. Any active member may link a chat to their own account; each call touches only the
/// caller's link in the current organization. Without a bot token every link call answers <c>telegram.not_configured</c>.
/// </summary>
public sealed class TelegramController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(TelegramRoutes.Status)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<TelegramStatusView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTelegramStatusQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(TelegramRoutes.LinkCode)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<TelegramLinkCodeView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateLinkCode(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTelegramLinkCodeCommand(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(TelegramRoutes.Link)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<TelegramLinkView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLink(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTelegramLinkQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(TelegramRoutes.Link)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlink(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnlinkTelegramCommand(), cancellationToken);
        return FromResult(result);
    }
}
