using Parley.Insights.Contracts.Rfm;

namespace Parley.Insights.Options;

/// <summary>Settings of the priority list. Bind to the <c>Insights:Priority</c> section; the weights are configuration, not code.</summary>
public sealed class InsightsPriorityOptions
{
    public const string SectionName = "Insights:Priority";

    public const int MaximumItemsPerOwner = 500;

    /// <summary>Weight of each RFM segment. Every scored segment must be listed, in the documented order.</summary>
    public Dictionary<string, decimal> Weights { get; set; } = [];

    /// <summary>Bonus of a contact with an open task that is due.</summary>
    public decimal TaskDueBonus { get; set; }

    /// <summary>Bonus of a contact with an open deal that has not moved for <see cref="StaleDealDays"/>.</summary>
    public decimal StaleDealBonus { get; set; }

    /// <summary>Days without an update after which an open deal counts as stale.</summary>
    public int StaleDealDays { get; set; }

    /// <summary>Contacts kept on the priority list of each owner.</summary>
    public int ItemsPerOwner { get; set; }

    /// <summary>ML mode: weight of the uplift term. Uplift lies between -1 and 1, so the weight sets its share of the value.</summary>
    public decimal UpliftWeight { get; set; } = 0.7m;

    /// <summary>ML mode: weight of the RFM segment term, the segment weight divided by the largest segment weight.</summary>
    public decimal RfmWeight { get; set; } = 0.3m;

    /// <summary>ML mode: an uplift at or above this mark earns the uplift reason.</summary>
    public decimal UpliftHighThreshold { get; set; } = 0.10m;

    /// <summary>The scored segments, in the order their weights must follow, from the highest weight to the lowest.</summary>
    public static IReadOnlyList<string> ScoredSegmentsInOrder { get; } =
    [
        RfmSegmentNames.AtRisk,
        RfmSegmentNames.NeedsAttention,
        RfmSegmentNames.Promising,
        RfmSegmentNames.Champions,
        RfmSegmentNames.Loyal,
        RfmSegmentNames.Regular,
        RfmSegmentNames.Hibernating,
        RfmSegmentNames.Lost,
    ];
}
