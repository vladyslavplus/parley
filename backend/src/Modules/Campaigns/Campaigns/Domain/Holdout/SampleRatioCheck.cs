using Parley.Campaigns.Shared;

namespace Parley.Campaigns.Domain.Holdout;

/// <summary>
/// Sample ratio mismatch check (domain model 7.3): a chi-squared test with one degree of freedom of the observed arm
/// counts against the configured holdout share. A small p-value means the assignment or the pipeline is broken, and
/// the arm comparison cannot be trusted.
/// </summary>
public static class SampleRatioCheck
{
    /// <summary>The two-sided p-value. Returns 1 when there is nothing to test.</summary>
    public static double PValue(long holdoutCount, long treatmentCount, int holdoutShareBps)
    {
        var total = (double)(holdoutCount + treatmentCount);
        if (total == 0)
        {
            return 1d;
        }

        var expectedHoldout = total * holdoutShareBps / BasisPoints.Scale;
        var expectedTreatment = total - expectedHoldout;
        if (expectedHoldout <= 0 || expectedTreatment <= 0)
        {
            return 1d;
        }

        var holdoutDeviation = holdoutCount - expectedHoldout;
        var treatmentDeviation = treatmentCount - expectedTreatment;
        var chiSquared = holdoutDeviation * holdoutDeviation / expectedHoldout
            + treatmentDeviation * treatmentDeviation / expectedTreatment;

        // One degree of freedom: P(chi2 > x) = erfc(sqrt(x / 2)).
        return ErrorFunction.Erfc(Math.Sqrt(chiSquared / 2));
    }
}
