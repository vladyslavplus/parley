using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Domain.Rfm;

namespace Parley.Insights.Domain.Priority;

/// <summary>
/// The RFM-mode priority of one contact: the weight of its segment plus a bonus for each open CRM fact, with the reason
/// codes that explain it. Pure functions; the weights and bonuses come from the caller's options.
/// </summary>
public static class PriorityRules
{
    /// <summary>Top tier of a quintile, the tier that earns the value and recency reasons.</summary>
    public const int TopTier = RfmTierCalculator.Buckets;

    public static PriorityScore Score(
        string segment,
        RfmTiers? tiers,
        PriorityFacts facts,
        IReadOnlyDictionary<string, decimal> weights,
        decimal taskDueBonus,
        decimal staleDealBonus)
    {
        ArgumentNullException.ThrowIfNull(weights);
        var weight = weights.TryGetValue(segment, out var segmentWeight) ? segmentWeight : 0m;
        var value = weight
            + (facts.HasDueTask ? taskDueBonus : 0m)
            + (facts.HasStaleDeal ? staleDealBonus : 0m);

        var reasons = new List<string>();
        if (segment == RfmSegmentNames.AtRisk)
        {
            reasons.Add(PriorityReasons.AtRisk);
        }

        if (facts.HasDueTask)
        {
            reasons.Add(PriorityReasons.TaskDue);
        }

        if (facts.HasStaleDeal)
        {
            reasons.Add(PriorityReasons.DealStale);
        }

        if (tiers is { M: TopTier })
        {
            reasons.Add(PriorityReasons.HighValue);
        }

        if (tiers is { R: TopTier })
        {
            reasons.Add(PriorityReasons.RecentOrder);
        }

        return new PriorityScore(value, reasons, ActionFor(segment, facts));
    }

    /// <summary>The next action: a stale deal is reviewed first, then a due task is called; otherwise the segment decides.</summary>
    public static string ActionFor(string segment, PriorityFacts facts)
    {
        if (facts.HasStaleDeal)
        {
            return PrioritySuggestedActions.ReviewDeal;
        }

        if (facts.HasDueTask)
        {
            return PrioritySuggestedActions.Call;
        }

        return segment switch
        {
            RfmSegmentNames.AtRisk or RfmSegmentNames.Hibernating or RfmSegmentNames.Lost => PrioritySuggestedActions.WinBack,
            RfmSegmentNames.Champions or RfmSegmentNames.Loyal => PrioritySuggestedActions.Reward,
            _ => PrioritySuggestedActions.Nurture,
        };
    }
}
