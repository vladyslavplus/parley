using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Telegram.Requests;
using Parley.Api.Localization;
using Parley.Api.RateLimiting;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Features.Telegram.Updates;
using Parley.Notifications.Options;
using Parley.Notifications.Telegram.Webhook;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Results;

namespace Parley.Api.Controllers.Telegram;

/// <summary>
/// The webhook that Telegram posts updates to in webhook mode. It takes no session: the secret token header that Telegram sends
/// with every update is the check, compared in constant time. The check runs before the body is read, so a caller without the
/// secret never costs a parse. Bodies above the size limit answer 413, and the route has its own rate limit. Without webhook mode
/// or a token the endpoint answers not found. Each update goes to the same handler polling uses.
/// </summary>
public sealed class TelegramWebhookController(
    TelegramUpdateDispatcher dispatcher,
    IOptions<TelegramOptions> options,
    ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpPost(TelegramRoutes.Webhook)]
    [PermissionOptOut(PermissionOptOutKind.Anonymous)]
    [EnableRateLimiting(TelegramRateLimitPolicies.Webhook)]
    [RequestSizeLimit(TelegramLimits.WebhookMaximumBodyBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!TelegramOptionRules.IsConfigured(settings) || settings.Mode != TelegramMode.Webhook)
        {
            return Problem(Error.NotFound(TelegramErrorCodes.NotConfigured));
        }

        var presented = Request.Headers[TelegramWebhookSecret.HeaderName].ToString();
        if (!TelegramWebhookSecret.Matches(presented, settings.WebhookSecret))
        {
            return Problem(Error.Forbidden(TelegramErrorCodes.WebhookSecretInvalid));
        }

        TelegramWebhookBody? body;
        try
        {
            body = await Request.ReadFromJsonAsync<TelegramWebhookBody>(cancellationToken);
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }
        catch (JsonException)
        {
            return Problem(Error.Validation(ErrorCodes.ValidationFailed));
        }

        if (body?.ToIncoming() is not { } update)
        {
            return Problem(Error.Validation(ErrorCodes.ValidationFailed));
        }

        await dispatcher.DispatchAsync(update, cancellationToken);
        return Ok();
    }
}
