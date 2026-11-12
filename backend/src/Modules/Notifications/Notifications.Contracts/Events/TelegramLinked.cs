using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Notifications.Contracts.Events;

/// <summary>A user linked a Telegram chat to the organization. Carries identifiers only.</summary>
public sealed record TelegramLinked : IntegrationEvent
{
    public Guid OrganizationId { get; init; }

    public Guid UserId { get; init; }
}
