using Mediator;
using Microsoft.Extensions.Logging;
using Parley.Notifications.Telegram.Bot;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.Telegram.Updates;

/// <summary>
/// The one entry for an update, used by polling and by the webhook. It runs the handling command, then sends the reply after the
/// command's transaction has committed. A failed reply is logged by its status only; the update itself is already handled.
/// </summary>
public sealed class TelegramUpdateDispatcher(
    IMediator mediator,
    ITelegramBotClient bot,
    TelegramSendPacer pacer,
    IMutableTenantContext tenantContext,
    ILogger<TelegramUpdateDispatcher> logger)
{
    public async Task DispatchAsync(TelegramIncomingUpdate update, CancellationToken cancellationToken)
    {
        var result = await HandleAsync(update, cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Telegram update refused. ErrorCode={ErrorCode}", result.Error?.Code);
            return;
        }

        if (result.Value?.Reply is not { } reply)
        {
            return;
        }

        await pacer.WaitTurnAsync(reply.ChatId, cancellationToken);
        try
        {
            await bot.SendTextAsync(reply.ChatId, reply.Text, cancellationToken);
        }
        catch (TelegramCallException exception)
        {
            logger.LogWarning("Telegram reply failed. StatusCode={StatusCode}", exception.StatusCode);
        }
    }

    private async Task<Result<TelegramUpdateResult>> HandleAsync(TelegramIncomingUpdate update, CancellationToken cancellationToken)
    {
        try
        {
            return await mediator.Send(new HandleTelegramUpdateCommand(update), cancellationToken);
        }
        finally
        {
            // The handling may have set the tenant of a linked organization; it must not outlive this update.
            tenantContext.Clear();
        }
    }
}
