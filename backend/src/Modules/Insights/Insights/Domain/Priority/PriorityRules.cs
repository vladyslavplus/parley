using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Domain.Rfm;

namespace Parley.Insights.Domain.Priority;

/// <summary>
/// The priority of one contact with the reason codes that explain it. Pure functions; the weights and bonuses come from the
/// caller's options. The RFM mode ranks by the segment weight and the CRM bonuses. The ML mode puts the uplift and the
/// normalized segment weight on the scale of the segment weights, so both terms and the CRM bonuses compare like the RFM
/// mode; a contact without an uplift score uses the same formula with an uplift of zero.
/// </summary>
public static class PriorityRules
{
    /// <summary>Top tier of a quintile, the tier that earns the value and recency reasons.</summary>
    public const int TopTier = RfmTierCalculator.Buckets;

    /// <summary>Score of a contact in the RFM mode. Its value is the segment weight plus the bonuses.</summary>
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
        var value = weight + CrmBonuses(facts, taskDueBonus, staleDealBonus);
        return new PriorityScore(value, Reasons(segment, tiers, facts, []), ActionFor(segment, facts));
    }

    /// <summary>
    /// Score of a contact in the ML mode, on the scale of the segment weights: the largest segment weight times the weighted
    /// uplift and the normalized segment weight, plus the CRM bonuses. A contact without an uplift score passes a zero
    /// uplift and gets the same scale. A contact the uplift model marks do-not-disturb stays on the list with a caution
    /// reason and a value of zero, so every contact with a positive value ranks above it; it is never dropped.
    /// </summary>
    public static PriorityScore ScoreMl(
        string rfmSegment,
        RfmTiers? tiers,
        PriorityFacts facts,
        decimal uplift,
        string upliftSegment,
        PriorityMlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var caution = upliftSegment == PriorityUpliftSegments.DoNotDisturb;
        var segmentWeight = options.Weights.TryGetValue(rfmSegment, out var weight) ? weight : 0m;
        var largest = options.Weights.Values.DefaultIfEmpty(0m).Max();
        var normalized = largest > 0m ? segmentWeight / largest : 0m;
        var value = largest * (options.UpliftWeight * uplift + options.RfmWeight * normalized)
            + CrmBonuses(facts, options.TaskDueBonus, options.StaleDealBonus);

        var extra = new List<string>(3);
        if (caution)
        {
            extra.Add(PriorityReasons.DoNotDisturb);

            // The list keeps the contact, ranked below every contact with a positive value.
            value = 0m;
        }

        if (upliftSegment == PriorityUpliftSegments.Persuadable)
        {
            extra.Add(PriorityReasons.Persuadable);
        }

        if (!caution && uplift >= options.UpliftHighThreshold)
        {
            extra.Add(PriorityReasons.UpliftHigh);
        }

        // A caution carries a hold action: contacting the contact now is exactly what the model advises against.
        var action = caution ? PrioritySuggestedActions.Hold : ActionFor(rfmSegment, facts);
        return new PriorityScore(value, Reasons(rfmSegment, tiers, facts, extra), action);
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

    private static decimal CrmBonuses(PriorityFacts facts, decimal taskDueBonus, decimal staleDealBonus) =>
        (facts.HasDueTask ? taskDueBonus : 0m) + (facts.HasStaleDeal ? staleDealBonus : 0m);

    /// <summary>The reasons in their fixed order: the model reasons first, then the segment, the CRM facts and the tiers.</summary>
    private static List<string> Reasons(string segment, RfmTiers? tiers, PriorityFacts facts, IReadOnlyList<string> leading)
    {
        var reasons = new List<string>(leading);
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

        return reasons;
    }
}
