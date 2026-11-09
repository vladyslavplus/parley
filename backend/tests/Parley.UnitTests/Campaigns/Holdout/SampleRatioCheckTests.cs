using AwesomeAssertions;
using Parley.Campaigns.Domain.Holdout;
using Xunit;

namespace Parley.UnitTests.Campaigns.Holdout;

/// <summary>The sample ratio check flags arm splits that contradict the configured share, and the MDE preview matches the domain example.</summary>
public sealed class SampleRatioCheckTests
{
    private const int HoldoutShareBps = 1000;

    private const long Treatment = 9000;

    private const long Holdout = 1000;

    private const double ExactSplitPValue = 1d;

    private const double ExactSplitTolerance = 1e-6;

    /// <summary>1100 holdout of 10000 at 10 percent: chi-squared 11.11, two-sided p about 0.00086.</summary>
    private const long SkewedHoldout = 1100;

    private const long SkewedTreatment = 8900;

    private const double SkewedLowerBound = 0.0007;

    private const double SkewedUpperBound = 0.001;

    /// <summary>Domain example: 590 holdout and 5310 treatment at a 20 percent baseline give 4.9 points.</summary>
    private const long ExampleHoldout = 590;

    private const long ExampleTreatment = 5310;

    private const double ExampleBaselineRate = 0.2;

    private const double ZAlpha = 1.959964;

    private const double ZBeta = 0.841621;

    private const double ExampleEffectPoints = 4.9;

    private const double ExampleRoundingPoints = 0.05;

    [Fact]
    public void PValue_IsOne_WhenTheSplitMatchesTheShare()
    {
        SampleRatioCheck.PValue(Holdout, Treatment, HoldoutShareBps).Should().BeApproximately(ExactSplitPValue, ExactSplitTolerance);
    }

    [Fact]
    public void PValue_IsBelowTheAlarm_WhenTheSplitIsSkewed()
    {
        var pValue = SampleRatioCheck.PValue(SkewedHoldout, SkewedTreatment, HoldoutShareBps);

        pValue.Should().BeInRange(SkewedLowerBound, SkewedUpperBound);
    }

    [Fact]
    public void MinimumDetectableEffect_MatchesTheDomainExample()
    {
        var effect = MinimumDetectableEffect.Compute(ExampleTreatment, ExampleHoldout, ExampleBaselineRate, ZAlpha, ZBeta);

        (effect * 100).Should().BeApproximately(ExampleEffectPoints, ExampleRoundingPoints);
    }
}
