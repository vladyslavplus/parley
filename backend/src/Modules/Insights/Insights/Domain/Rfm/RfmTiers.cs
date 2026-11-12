namespace Parley.Insights.Domain.Rfm;

/// <summary>The three quintile tiers of a contact, 1 (lowest) to 5 (highest) on each axis.</summary>
/// <param name="R">Recency tier: 5 is the most recent last order.</param>
/// <param name="F">Frequency tier: 5 is the most orders in the last 365 days.</param>
/// <param name="M">Monetary tier: 5 is the highest value in the last 365 days.</param>
public readonly record struct RfmTiers(int R, int F, int M);
