using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Campaigns.Contracts.Events;

/// <summary>
/// Every targeted treatment recipient of a campaign has a final send outcome. Carries identifiers only, plus the user who
/// started the campaign (null for a campaign started before the starter was recorded).
/// </summary>
public sealed record CampaignCompleted : IntegrationEvent
{
    public Guid CampaignId { get; init; }

    public DateTimeOffset CompletedAt { get; init; }

    public Guid? StartedByUserId { get; init; }
}
