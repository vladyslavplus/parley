using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Notifications.Requests;
using Parley.Api.Localization;
using Parley.Notifications.Features.List;
using Parley.Notifications.Features.MarkAllRead;
using Parley.Notifications.Features.MarkRead;
using Parley.Notifications.Features.Preferences.Get;
using Parley.Notifications.Features.Preferences.Update;
using Parley.Notifications.Features.Shared;
using Parley.Notifications.Features.UnreadCount;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Api.Controllers.Notifications;

/// <summary>
/// The signed-in user's in-app notifications and their preferences. These are personal endpoints: any active member of the
/// organization may call them, and each call reads and changes only the caller's own notifications.
/// </summary>
public sealed class NotificationsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(NotificationsRoutes.List)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<KeysetCursorPage<NotificationView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] bool unreadOnly,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListNotificationsQuery(after, limit, unreadOnly), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(NotificationsRoutes.UnreadCount)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<UnreadCountView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUnreadCountQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(NotificationsRoutes.MarkRead)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<NotificationView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(NotificationsRoutes.MarkAllRead)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<ReadAllView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(NotificationsRoutes.Preferences)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<NotificationPreferencesView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetNotificationPreferencesQuery(), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(NotificationsRoutes.Preferences)]
    [PermissionOptOut(PermissionOptOutKind.AuthenticatedOnly)]
    [ProducesResponseType<NotificationPreferencesView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest? request,
        CancellationToken cancellationToken)
    {
        if (request?.Version is not { } version || request.Items is not { } items)
        {
            return Problem(Error.Validation(ErrorCodes.ValidationFailed));
        }

        var changes = new List<PreferenceChange>(items.Count);
        foreach (var item in items)
        {
            if (item.Type is null || item.Channel is null || item.Enabled is not { } enabled)
            {
                return Problem(Error.Validation(ErrorCodes.ValidationFailed));
            }

            changes.Add(new PreferenceChange(item.Type, item.Channel, enabled));
        }

        var result = await mediator.Send(new UpdateNotificationPreferencesCommand(version, changes), cancellationToken);
        return FromResult(result, Ok);
    }
}
