using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Campaigns.Domain.Attribution;

/// <summary>
/// What one targeted recipient did in the attribution window of its campaign. One row per frozen recipient, in either
/// arm, written by the attribution job. A recipient without purchases still has a row, with zero values, so the
/// denominators of the arm rates are complete (intention to treat).
/// </summary>
public sealed class RecipientOutcome : BusinessEntity
{
    public Guid CampaignId { get; set; }

    public Guid RecipientId { get; set; }

    public Guid ContactId { get; set; }

    /// <summary>True when the recipient made a conversion in the window (a first non-cancellation order, or a won deal for the won-deal rule).</summary>
    public bool Converted { get; set; }

    /// <summary>Instant of the first conversion in the window, UTC; null without one.</summary>
    public DateTime? FirstConversionAt { get; set; }

    /// <summary>Net value of all orders of the recipient in the window (cancellations subtract). Zero without orders.</summary>
    public decimal Revenue { get; set; }

    /// <summary>Non-cancellation orders in the window.</summary>
    public int OrdersInWindow { get; set; }

    /// <summary>Orders in the window that the demo seed simulated. Non-zero only in demo campaigns.</summary>
    public int SimulatedOrdersInWindow { get; set; }

    /// <summary>When the attribution job last wrote the row, UTC.</summary>
    public DateTime EvaluatedAt { get; set; }
}
