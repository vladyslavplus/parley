using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Insights.Domain.Rfm;

/// <summary>One contact's RFM state on one organization day. The tiers are null for a contact without orders.</summary>
public sealed class RfmSnapshot : BusinessEntity
{
    public Guid ContactId { get; set; }

    public Guid? OwnerUserId { get; set; }

    /// <summary>The organization's calendar day the snapshot is computed for.</summary>
    public DateOnly AsOf { get; set; }

    public int? RecencyDays { get; set; }

    /// <summary>Orders in the last 365 days; the frequency axis.</summary>
    public int Frequency { get; set; }

    /// <summary>Value in the last 365 days; the monetary axis.</summary>
    public decimal Monetary { get; set; }

    public int? RTier { get; set; }

    public int? FTier { get; set; }

    public int? MTier { get; set; }

    public string Segment { get; set; } = string.Empty;
}
