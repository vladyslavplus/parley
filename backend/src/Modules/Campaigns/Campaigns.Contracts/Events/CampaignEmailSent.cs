using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Campaigns.Contracts.Events;

/// <summary>
/// A campaign email was accepted by the mail server. Identifiers and the message id only; the address and the body
/// never leave the campaigns module.
/// </summary>
public sealed record CampaignEmailSent : IntegrationEvent
{
    public Guid CampaignId { get; init; }

    public Guid ContactId { get; init; }

    public string MessageId { get; init; } = string.Empty;

    public DateTimeOffset SentAt { get; init; }
}
