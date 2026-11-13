namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Purchase archetypes of the generated workspace. Each contact takes one from a deterministic bucket, so the same seed
/// always gives the same shape.
/// </summary>
public enum DemoArchetype
{
    Champions,
    Loyal,
    Promising,
    NeedsAttention,
    AtRisk,
    Hibernating,
    Lost,
    Regular,
    NoOrders,
}
