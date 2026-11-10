using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Campaigns.Contracts.Events;

/// <summary>A campaign left <c>ready</c> and its send job was queued. Carries identifiers and the start instant only.</summary>
public sealed record CampaignStarted : IntegrationEvent
{
    public Guid CampaignId { get; init; }

    public DateTimeOffset StartedAt { get; init; }
}
