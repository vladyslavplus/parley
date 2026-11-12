using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parley.Notifications.Options;

namespace Parley.Notifications.Telegram.Delivery;

/// <summary>
/// Runs the delivery sweep in a loop. When a batch is short, the worker waits briefly. Once an hour it also removes update ids
/// past their retention. The worker runs only when the channel has a token and the delivery worker is enabled.
/// </summary>
public sealed class TelegramDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    TelegramDeliverySweep sweep,
    IOptions<TelegramOptions> options,
    TimeProvider timeProvider,
    ILogger<TelegramDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan FailureInterval = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan RetentionInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!TelegramOptionRules.IsConfigured(settings) || !settings.DeliveryWorkerEnabled)
        {
            return;
        }

        var nextRetention = timeProvider.GetUtcNow();
        while (!stoppingToken.IsCancellationRequested)
        {
            int claimed;
            try
            {
                claimed = await sweep.RunOnceAsync(stoppingToken);
                if (timeProvider.GetUtcNow() >= nextRetention)
                {
                    await RemoveExpiredUpdatesAsync(settings, stoppingToken);
                    nextRetention = timeProvider.GetUtcNow() + RetentionInterval;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError("Telegram delivery sweep failed. ExceptionType={ExceptionType}", exception.GetType().Name);
                await Task.Delay(FailureInterval, timeProvider, stoppingToken);
                continue;
            }

            if (claimed < settings.DeliveryBatchSize)
            {
                await Task.Delay(IdleInterval, timeProvider, stoppingToken);
            }
        }
    }

    private async Task RemoveExpiredUpdatesAsync(TelegramOptions settings, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var retention = scope.ServiceProvider.GetRequiredService<TelegramUpdateRetention>();
        await retention.RemoveOlderThanAsync(TimeSpan.FromDays(settings.UpdateRetentionDays), cancellationToken);
    }
}
