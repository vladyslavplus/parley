using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Campaigns.Domain.Attribution;

/// <summary>
/// The stored result of one campaign: counts per arm, the statistics of the comparison, and the window. Rebuilt by the
/// attribution job; one row per campaign. The statistics are null where the data cannot define them. The row says whether
/// the window was closed when it was computed; the reader recomputes the window state from the campaign, so a closed
/// window is reported closed even before the next run.
/// </summary>
public sealed class CampaignResult : BusinessEntity
{
    public Guid CampaignId { get; set; }

    /// <summary>Frozen recipients per arm, targeted or not. The sample ratio check uses these (assignment happens before targeting).</summary>
    public long TreatmentAssigned { get; set; }

    public long HoldoutAssigned { get; set; }

    /// <summary>Targeted recipients per arm: the population the comparison measures (domain model 7.4).</summary>
    public long TreatmentTargeted { get; set; }

    public long HoldoutTargeted { get; set; }

    public long TreatmentConverted { get; set; }

    public long HoldoutConverted { get; set; }

    public decimal TreatmentRevenue { get; set; }

    public decimal HoldoutRevenue { get; set; }

    public double? RateTreatment { get; set; }

    public double? RateHoldout { get; set; }

    public double? Lift { get; set; }

    public double? LiftCiLow { get; set; }

    public double? LiftCiHigh { get; set; }

    public double? PValue { get; set; }

    public double? SrmPValue { get; set; }

    public double? MinimumDetectableEffect { get; set; }

    public double? IncrementalRevenue { get; set; }

    /// <summary>True when any targeted recipient's window holds a simulated order. The result is then labeled simulated everywhere.</summary>
    public bool Simulated { get; set; }

    /// <summary>End of the attribution window: the campaign's completion plus the window length.</summary>
    public DateTime WindowEndsAt { get; set; }

    /// <summary>Whether the window was closed when the row was computed.</summary>
    public bool WindowClosed { get; set; }

    public DateTime ComputedAt { get; set; }
}
