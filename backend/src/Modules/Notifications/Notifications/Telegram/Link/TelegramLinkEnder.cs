using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Contracts.Events;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Outbox.Publishing;

namespace Parley.Notifications.Telegram.Link;

/// <summary>
/// Ends one Telegram link: switches it off, cancels the chat's pending messages for that user in that organization, and
/// publishes <see cref="TelegramUnlinked"/>. The tenant must already be the link's organization. The caller saves.
/// </summary>
public sealed class TelegramLinkEnder(
    NotificationsDbContext dbContext,
    IModuleOutbox<NotificationsDbContext> outbox,
    TimeProvider timeProvider)
{
    public async Task EndAsync(TelegramLink link, CancellationToken cancellationToken)
    {
        link.Active = false;
        link.DeactivatedAt = timeProvider.GetUtcNow().UtcDateTime;
        outbox.Add(new TelegramUnlinked { OrganizationId = link.OrganizationId, UserId = link.UserId });

        await dbContext.TelegramDeliveries
            .Where(delivery => delivery.OrganizationId == link.OrganizationId
                && delivery.UserId == link.UserId
                && delivery.ChatId == link.ChatId
                && delivery.Status == TelegramDeliveryStatuses.Pending)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(delivery => delivery.Status, TelegramDeliveryStatuses.Cancelled),
                cancellationToken);
    }
}
