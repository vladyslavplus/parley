using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Persistence;

namespace Parley.Notifications.Telegram.Delivery;

/// <summary>Removes handled Telegram update ids past their retention. Ids older than the window can no longer be replayed.</summary>
public sealed class TelegramUpdateRetention(NotificationsDbContext dbContext, TimeProvider timeProvider)
{
    public Task<int> RemoveOlderThanAsync(TimeSpan retention, CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - retention;
        return dbContext.TelegramProcessedUpdates
            .Where(update => update.ReceivedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
