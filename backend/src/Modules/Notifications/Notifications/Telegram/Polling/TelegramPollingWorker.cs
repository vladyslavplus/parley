using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parley.Notifications.Features.Telegram.Updates;
using Parley.Notifications.Options;
using Parley.Notifications.Telegram.Bot;

namespace Parley.Notifications.Telegram.Polling;

/// <summary>
/// Reads the bot's updates by long polling and feeds each one to the same handler the webhook uses. The offset moves only after an
/// update is handled. A failed update is retried on the next poll until the attempt limit; then it is skipped, and the failure is
/// logged with the update id and the exception type only. Failures of the poll itself back off exponentially up to the configured
/// maximum. The loop stops when the host stops. The worker runs only in polling mode with a token.
/// </summary>
public sealed class TelegramPollingWorker(
    IServiceScopeFactory scopeFactory,
    ITelegramBotClient bot,
    IOptions<TelegramOptions> options,
    ILogger<TelegramPollingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan FirstBackoff = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!TelegramOptionRules.IsConfigured(settings) || settings.Mode != TelegramMode.Polling)
        {
            return;
        }

        var cursor = new TelegramPollingCursor();
        var backoff = TimeSpan.Zero;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await bot.GetUpdatesAsync(cursor.Offset, settings.PollingTimeoutSeconds, stoppingToken);
                var completed = await TelegramPollingBatch.HandleAsync(
                    updates,
                    cursor,
                    settings.MaximumUpdateAttempts,
                    DispatchAsync,
                    (updateId, attempt, exception) => logger.LogWarning(
                        "Telegram update failed. UpdateId={UpdateId} Attempt={Attempt} ExceptionType={ExceptionType}",
                        updateId,
                        attempt,
                        exception.GetType().Name),
                    stoppingToken);

                if (completed)
                {
                    backoff = TimeSpan.Zero;
                    continue;
                }

                // The batch stopped at a failing update: pause, then poll again from that update.
                backoff = NextBackoff(backoff, settings.PollingMaximumBackoff);
                await Task.Delay(backoff, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                backoff = NextBackoff(backoff, settings.PollingMaximumBackoff);
                logger.LogWarning(
                    "Telegram polling failed. ExceptionType={ExceptionType} BackoffSeconds={BackoffSeconds}",
                    exception.GetType().Name,
                    backoff.TotalSeconds);
                await Task.Delay(backoff, stoppingToken);
            }
        }
    }

    /// <summary>Handles one update in its own scope, so its tenant and unit of work end with it.</summary>
    private async Task DispatchAsync(TelegramIncomingUpdate update, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TelegramUpdateDispatcher>().DispatchAsync(update, cancellationToken);
    }

    private static TimeSpan NextBackoff(TimeSpan current, TimeSpan maximum)
    {
        var next = current == TimeSpan.Zero ? FirstBackoff : current + current;
        return next > maximum ? maximum : next;
    }
}
