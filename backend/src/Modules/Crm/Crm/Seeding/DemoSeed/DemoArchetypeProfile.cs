namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>Order pattern of one archetype: count range, age of the last order, amount range and the gap between orders.</summary>
internal sealed record DemoArchetypeProfile(
    DemoArchetype Archetype,
    int SharePercent,
    int MinimumOrders,
    int MaximumOrders,
    int MinimumLastOrderDays,
    int MaximumLastOrderDays,
    decimal MinimumAmount,
    decimal MaximumAmount,
    int MinimumGapDays,
    int MaximumGapDays);
