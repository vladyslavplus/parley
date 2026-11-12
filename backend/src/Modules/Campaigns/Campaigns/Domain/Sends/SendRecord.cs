using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Campaigns.Domain.Sends;

/// <summary>
/// The send claim of one targeted treatment recipient. The unique key on campaign and contact is the guard against a
/// second send: a recipient has at most one claim, and a retry re-uses that row.
/// </summary>
public sealed class SendRecord : BusinessEntity
{
    public Guid CampaignId { get; set; }

    public Guid ContactId { get; set; }

    public Guid RecipientId { get; set; }

    /// <summary><see cref="SendStatuses"/> value.</summary>
    public string Status { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public DateTime ClaimedAt { get; set; }

    /// <summary>End of the lease of a <c>claimed</c> row; null once the row has an outcome.</summary>
    public DateTime? LeaseExpiresAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string MessageId { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }
}
