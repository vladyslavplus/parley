using Parley.Insights.Contracts.Rfm;

namespace Parley.Insights.Domain.Rfm;

/// <summary>
/// The segment rule table of domain model 8. The first matching rule wins, in the order written here. A contact without
/// tiers has no orders and is <see cref="RfmSegmentNames.NoOrders"/>.
/// </summary>
public static class RfmSegmentRules
{
    /// <summary>The segment of a contact with tiers.</summary>
    public static string Classify(RfmTiers tiers)
    {
        var (r, f, m) = (tiers.R, tiers.F, tiers.M);
        if (r >= 4 && f >= 4 && m >= 4)
        {
            return RfmSegmentNames.Champions;
        }

        if (r >= 3 && f >= 4)
        {
            return RfmSegmentNames.Loyal;
        }

        if (r >= 4 && f <= 2)
        {
            return RfmSegmentNames.Promising;
        }

        if (r == 3 && f == 3)
        {
            return RfmSegmentNames.NeedsAttention;
        }

        if (r <= 2 && f >= 3)
        {
            return RfmSegmentNames.AtRisk;
        }

        if (r <= 2 && f <= 2 && m >= 3)
        {
            return RfmSegmentNames.Hibernating;
        }

        if (r == 1)
        {
            return RfmSegmentNames.Lost;
        }

        return RfmSegmentNames.Regular;
    }

    /// <summary>The segment of a contact, given its tiers when it has orders.</summary>
    public static string Classify(RfmTiers? tiers) => tiers is { } present ? Classify(present) : RfmSegmentNames.NoOrders;
}
