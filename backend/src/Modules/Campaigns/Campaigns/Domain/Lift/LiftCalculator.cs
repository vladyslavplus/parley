using Parley.Campaigns.Domain.Holdout;
using Parley.Campaigns.Shared;

namespace Parley.Campaigns.Domain.Lift;

/// <summary>
/// Campaign lift statistics (domain model 7.3 and 7.7). The definitions are the ones of the reference module
/// <c>ml/parley_ml/experiments/lift.py</c>; the committed vectors in <c>ml/tests/fixtures/lift_vectors.json</c> are the
/// contract between the two. Critical values come from the options and are exact quantiles. Undefined values are null.
/// </summary>
public static class LiftCalculator
{
    private const int BasisPointsScale = (int)BasisPoints.Scale;

    public static LiftResult Analyze(LiftArm treatment, LiftArm holdout, int shareTreatmentBps, double zAlpha, double zBeta)
    {
        ArgumentNullException.ThrowIfNull(treatment);
        ArgumentNullException.ThrowIfNull(holdout);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(shareTreatmentBps, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(shareTreatmentBps, BasisPointsScale);

        var rateTreatment = Rate(treatment.Conversions, treatment.Count);
        var rateHoldout = Rate(holdout.Conversions, holdout.Count);
        var lift = rateTreatment is { } pt && rateHoldout is { } ph ? pt - ph : (double?)null;
        var interval = NewcombeWilson(treatment, holdout, zAlpha);

        var srm = treatment.Count + holdout.Count == 0
            ? (double?)null
            : SampleRatioCheck.PValue(holdout.Count, treatment.Count, BasisPointsScale - shareTreatmentBps);

        var mde = treatment.Count > 0 && holdout.Count > 0 && rateHoldout is { } baseline
            ? MinimumDetectableEffect.Compute(treatment.Count, holdout.Count, baseline, zAlpha, zBeta)
            : (double?)null;

        return new LiftResult(
            rateTreatment,
            rateHoldout,
            lift,
            interval?.Low,
            interval?.High,
            TwoProportionZPValue(treatment, holdout),
            srm,
            mde,
            IncrementalRevenue(treatment, holdout));
    }

    /// <summary>conversions / n, or null when n is zero.</summary>
    public static double? Rate(long conversions, long count) => count == 0 ? null : (double)conversions / count;

    /// <summary>Wilson score interval of one proportion at quantile <paramref name="z"/>; null when n is zero.</summary>
    public static (double Low, double High)? WilsonInterval(long conversions, long count, double z)
    {
        if (count == 0)
        {
            return null;
        }

        var p = (double)conversions / count;
        var z2 = z * z;
        var denominator = 1d + z2 / count;
        var centre = (p + z2 / (2d * count)) / denominator;
        var half = z * Math.Sqrt(p * (1d - p) / count + z2 / (4d * count * count)) / denominator;
        return (centre - half, centre + half);
    }

    /// <summary>Newcombe 1998, method 10 (hybrid score): the interval of p_t - p_h. Null when either arm is empty.</summary>
    public static (double Lift, double Low, double High)? NewcombeWilson(LiftArm treatment, LiftArm holdout, double z)
    {
        var rateTreatment = Rate(treatment.Conversions, treatment.Count);
        var rateHoldout = Rate(holdout.Conversions, holdout.Count);
        var wilsonTreatment = WilsonInterval(treatment.Conversions, treatment.Count, z);
        var wilsonHoldout = WilsonInterval(holdout.Conversions, holdout.Count, z);
        if (rateTreatment is not { } pt || rateHoldout is not { } ph || wilsonTreatment is not { } wt || wilsonHoldout is not { } wh)
        {
            return null;
        }

        var lift = pt - ph;
        var (lt, ut) = wt;
        var (lh, uh) = wh;
        var low = lift - Math.Sqrt((pt - lt) * (pt - lt) + (uh - ph) * (uh - ph));
        var high = lift + Math.Sqrt((ut - pt) * (ut - pt) + (ph - lh) * (ph - lh));
        return (lift, low, high);
    }

    /// <summary>Two-sided pooled two-proportion z-test. Null when an arm is empty or the pooled variance is zero.</summary>
    public static double? TwoProportionZPValue(LiftArm treatment, LiftArm holdout)
    {
        var total = treatment.Count + holdout.Count;
        if (treatment.Count == 0 || holdout.Count == 0 || total == 0)
        {
            return null;
        }

        var pooled = (double)(treatment.Conversions + holdout.Conversions) / total;
        var variance = pooled * (1d - pooled) * (1d / treatment.Count + 1d / holdout.Count);
        if (variance == 0d)
        {
            return null;
        }

        var pt = (double)treatment.Conversions / treatment.Count;
        var ph = (double)holdout.Conversions / holdout.Count;
        var z = (pt - ph) / Math.Sqrt(variance);

        // 2 (1 - Phi(|z|)) written as erfc(|z| / sqrt 2): the same value, without the cancellation of 1 - Phi in the tail.
        return ErrorFunction.Erfc(Math.Abs(z) / Math.Sqrt(2d));
    }

    /// <summary>(mean revenue treatment minus mean revenue holdout) times the treatment count; null when a mean is undefined.</summary>
    public static double? IncrementalRevenue(LiftArm treatment, LiftArm holdout)
    {
        if (treatment.Count == 0 || holdout.Count == 0)
        {
            return null;
        }

        var meanTreatment = treatment.RevenueSum / treatment.Count;
        var meanHoldout = holdout.RevenueSum / holdout.Count;
        return (meanTreatment - meanHoldout) * treatment.Count;
    }
}
