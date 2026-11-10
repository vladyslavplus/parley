using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Campaigns.Contracts.Events;

/// <summary>A recipient followed an unsubscribe link and the address was suppressed. Identifiers and the instant only.</summary>
public sealed record RecipientUnsubscribed : IntegrationEvent
{
    public Guid CampaignId { get; init; }

    public Guid ContactId { get; init; }

    public DateTimeOffset SuppressedAt { get; init; }
}
