using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parley.Notifications.Options;

namespace Parley.Notifications.Telegram.Delivery;

/// <summary>
/// One pass over the due deliveries: claim a batch, then send each message in its own scope. Each send is isolated, so one
/// crashing delivery does not stop the batch, and its lease ends on its own. The background worker calls this in a loop; a test
/// calls it once, so the outcome is deterministic.
/// </summary>
public sealed class TelegramDeliverySweep(
    IServiceScopeFactory scopeFactory,
    IOptions<TelegramOptions> options,
    TimeProvider timeProvider,
    ILogger<TelegramDeliverySweep> logger)
{
    /// <summary>Claims one batch and attempts each delivery. Returns how many deliveries the batch claimed.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        List<ClaimedTelegramDelivery> claimed;
        using (var scope = scopeFactory.CreateScope())
        {
            var claimer = scope.ServiceProvider.GetRequiredService<TelegramDeliveryClaimer>();
            var now = timeProvider.GetUtcNow().UtcDateTime;
            claimed = await claimer.ClaimAsync(now, settings.DeliveryClaimLease, settings.DeliveryBatchSize, cancellationToken);
        }

        foreach (var delivery in claimed)
        {
            using var unit = scopeFactory.CreateScope();
            var attempt = unit.ServiceProvider.GetRequiredService<TelegramDeliveryAttempt>();
            try
            {
                await attempt.RunAsync(delivery, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The attempt was counted before the crash; the lease ends on its own and the delivery comes back, until the
                // attempts run out and the next attempt fails the row.
                logger.LogError("Telegram delivery attempt crashed. ExceptionType={ExceptionType}", exception.GetType().Name);
            }
        }

        return claimed.Count;
    }
}
