namespace Parley.Insights.Contracts.Rfm;

/// <summary>RFM segment names (domain model 8). Stored as lowercase text; the order below is the rule order of the table.</summary>
public static class RfmSegmentNames
{
    public const string Champions = "champions";

    public const string Loyal = "loyal";

    public const string Promising = "promising";

    public const string NeedsAttention = "needs_attention";

    public const string AtRisk = "at_risk";

    public const string Hibernating = "hibernating";

    public const string Lost = "lost";

    public const string Regular = "regular";

    public const string NoOrders = "no_orders";

    /// <summary>Every segment name, in the order of the rule table with <see cref="NoOrders"/> last.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Champions, Loyal, Promising, NeedsAttention, AtRisk, Hibernating, Lost, Regular, NoOrders,
    ];
}
