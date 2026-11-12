using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Domain.Notifications;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Persistence;

namespace Parley.Notifications.Telegram.Delivery;

/// <summary>
/// Stages one pending delivery for each active chat of the recipient in the notification's organization. It runs inside the
/// consumer's transaction and never saves. The worker sends the rows later, outside any transaction.
/// </summary>
public sealed class TelegramDeliveryPlanner(NotificationsDbContext dbContext, TimeProvider timeProvider)
{
    public async Task PlanAsync(Notification notification, CancellationToken cancellationToken)
    {
        var chats = await dbContext.TelegramLinks
            .AsNoTracking()
            .Where(link => link.UserId == notification.UserId && link.Active)
            .Select(link => link.ChatId)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var chatId in chats)
        {
            dbContext.TelegramDeliveries.Add(new TelegramDelivery
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = notification.OrganizationId,
                NotificationId = notification.Id,
                UserId = notification.UserId,
                ChatId = chatId,
                Status = TelegramDeliveryStatuses.Pending,
                Attempts = 0,
                NextAttemptAt = now,
                CreatedAt = now,
            });
        }
    }
}
