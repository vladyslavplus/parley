using System.Globalization;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// The share table of the purchase archetypes. Change it together with <see cref="DemoArchetypeProfile"/> when the RFM
/// segment rules change, so every named segment stays populated after the real quintile snapshot runs.
/// </summary>
internal static class DemoArchetypes
{
    /// <summary>
    /// The mix, in lookup order. The shares add up to 100. Champions, loyal, promising, needs-attention, at-risk,
    /// hibernating, lost and regular are every named segment; no-orders covers the contact without a purchase.
    /// </summary>
    public static IReadOnlyList<DemoArchetypeProfile> Mix { get; } =
    [
        new(DemoArchetype.Champions, 8, 18, 30, 0, 20, 80m, 350m, 18, 35),
        new(DemoArchetype.Loyal, 12, 10, 18, 20, 60, 60m, 220m, 25, 45),
        new(DemoArchetype.Promising, 13, 1, 2, 0, 25, 40m, 180m, 30, 60),
        new(DemoArchetype.NeedsAttention, 12, 4, 8, 45, 90, 50m, 200m, 30, 60),
        new(DemoArchetype.AtRisk, 12, 6, 14, 100, 240, 40m, 180m, 25, 50),
        new(DemoArchetype.Hibernating, 8, 1, 2, 100, 240, 150m, 450m, 60, 120),
        new(DemoArchetype.Lost, 12, 1, 3, 300, 700, 30m, 140m, 60, 120),
        new(DemoArchetype.Regular, 18, 2, 5, 30, 90, 40m, 160m, 40, 80),
        new(DemoArchetype.NoOrders, 5, 0, 0, 0, 0, 0m, 0m, 0, 0),
    ];

    /// <summary>Profile of the numbered contact, by a deterministic bucket over the share table.</summary>
    public static DemoArchetypeProfile For(int contactIndex)
    {
        var bucket = DemoIdentifiers.Bucket(contactIndex.ToString(CultureInfo.InvariantCulture), "archetype", 100);
        var cumulative = 0;
        foreach (var profile in Mix)
        {
            cumulative += profile.SharePercent;
            if (bucket < cumulative)
            {
                return profile;
            }
        }

        return Mix[^1];
    }
}
