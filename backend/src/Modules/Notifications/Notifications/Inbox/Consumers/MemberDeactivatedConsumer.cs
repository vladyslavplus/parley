using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Events;
using Parley.Notifications.Persistence;
using Parley.Notifications.Telegram.Link;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Notifications.Inbox.Consumers;

/// <summary>
/// Ends the Telegram links of a deactivated member in the organization, and cancels their pending messages for those chats. A
/// deactivated member no longer has access to what the messages describe, so their chats must stop receiving them.
/// </summary>
public sealed class MemberDeactivatedConsumer(NotificationsDbContext dbContext, TelegramLinkEnder linkEnder)
    : IIntegrationEventConsumer<MemberDeactivated>
{
    public async Task ConsumeAsync(MemberDeactivated integrationEvent, CancellationToken cancellationToken)
    {
        var links = await dbContext.TelegramLinks
            .Where(link => link.UserId == integrationEvent.UserId && link.Active)
            .ToListAsync(cancellationToken);
        if (links.Count == 0)
        {
            return;
        }

        foreach (var link in links)
        {
            await linkEnder.EndAsync(link, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
