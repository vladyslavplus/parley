namespace Parley.Campaigns.Domain.Holdout;

/// <summary>
/// Minimum detectable effect of a two-proportion comparison (domain model 7.7): the smallest absolute difference in
/// conversion rates the arms can detect at the given alpha and power. Small holdouts detect only large effects.
/// </summary>
public static class MinimumDetectableEffect
{
    /// <summary>The effect as a proportion (0.049 means 4.9 percentage points). Both counts must be positive.</summary>
    public static double Compute(long treatmentCount, long holdoutCount, double baselineRate, double zAlpha, double zBeta)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(treatmentCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(holdoutCount);

        var variance = baselineRate * (1 - baselineRate) * (1d / treatmentCount + 1d / holdoutCount);
        return (zAlpha + zBeta) * Math.Sqrt(variance);
    }
}
