using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Notifications.Contracts.Events;

/// <summary>A Telegram link of the organization ended: the user unlinked it, the chat blocked the bot, or the membership ended.</summary>
public sealed record TelegramUnlinked : IntegrationEvent
{
    public Guid OrganizationId { get; init; }

    public Guid UserId { get; init; }
}
