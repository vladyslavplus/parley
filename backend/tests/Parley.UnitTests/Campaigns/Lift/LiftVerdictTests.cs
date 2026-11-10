using AwesomeAssertions;
using Parley.Campaigns.Domain.Lift;
using Xunit;

namespace Parley.UnitTests.Campaigns.Lift;

/// <summary>
/// The verdict table: nothing is claimed until the result is final, a sample ratio alarm makes a final result unreliable, and
/// a lift is claimed only with a final result, a sound sample ratio and an interval that excludes zero.
/// </summary>
public sealed class LiftVerdictTests
{
    private const double SrmAlarmP = 0.001;

    [Fact]
    public void OpenWindow_IsTooEarly_EvenWithAClearInterval()
    {
        LiftVerdict.Decide(isFinal: false, srmPValue: 0.5, SrmAlarmP, ciLow: 0.05, ciHigh: 0.2)
            .Should().Be(LiftVerdict.TooEarly);
    }

    [Fact]
    public void FinalResult_WithIntervalAboveZero_IsMeasurableLift()
    {
        LiftVerdict.Decide(isFinal: true, srmPValue: 0.5, SrmAlarmP, ciLow: 0.01, ciHigh: 0.08)
            .Should().Be(LiftVerdict.MeasurableLift);
    }

    [Fact]
    public void FinalResult_WithIntervalBelowZero_IsMeasurableLift_WithANegativeSign()
    {
        LiftVerdict.Decide(isFinal: true, srmPValue: 0.5, SrmAlarmP, ciLow: -0.08, ciHigh: -0.01)
            .Should().Be(LiftVerdict.MeasurableLift);
    }

    [Fact]
    public void FinalResult_WithIntervalAroundZero_IsNoMeasurableEffect()
    {
        LiftVerdict.Decide(isFinal: true, srmPValue: 0.5, SrmAlarmP, ciLow: -0.02, ciHigh: 0.03)
            .Should().Be(LiftVerdict.NoMeasurableEffect);
    }

    [Fact]
    public void AlarmingSampleRatio_IsUnreliable_EvenWhenTheIntervalExcludesZero()
    {
        LiftVerdict.Decide(isFinal: true, srmPValue: 0.0001, SrmAlarmP, ciLow: 0.01, ciHigh: 0.08)
            .Should().Be(LiftVerdict.Unreliable);
    }

    [Fact]
    public void AlarmingSampleRatio_IsUnreliable_EvenWhenTheIntervalAroundZero()
    {
        LiftVerdict.Decide(isFinal: true, srmPValue: 0.0001, SrmAlarmP, ciLow: -0.02, ciHigh: 0.03)
            .Should().Be(LiftVerdict.Unreliable);
    }

    [Fact]
    public void UndefinedInterval_IsNoMeasurableEffect()
    {
        LiftVerdict.Decide(isFinal: true, srmPValue: null, SrmAlarmP, ciLow: null, ciHigh: null)
            .Should().Be(LiftVerdict.NoMeasurableEffect);
    }
}

/// <summary>
/// Finality of a stored result: the figures count as final only when computed at or after the window end, so a closed window
/// with earlier figures stays provisional.
/// </summary>
public sealed class ResultStatusTests
{
    private static readonly DateTime WindowEndsAt = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ClosedWindow_WithFiguresComputedBeforeTheEnd_IsTooEarly_AndAwaitsFinal()
    {
        var computedAt = WindowEndsAt.AddDays(-4);
        var now = WindowEndsAt.AddDays(1);

        ResultStatus.IsFinal(WindowEndsAt, computedAt).Should().BeFalse();
        ResultStatus.WindowState(WindowEndsAt, computedAt, now).Should().Be(ResultStatus.AwaitingFinal);
        LiftVerdict.Decide(ResultStatus.IsFinal(WindowEndsAt, computedAt), 0.5, 0.001, 0.01, 0.08).Should().Be(LiftVerdict.TooEarly);
    }

    [Fact]
    public void ClosedWindow_WithFiguresComputedAtTheEnd_IsFinal()
    {
        ResultStatus.IsFinal(WindowEndsAt, WindowEndsAt).Should().BeTrue();
        ResultStatus.WindowState(WindowEndsAt, WindowEndsAt, WindowEndsAt.AddDays(1)).Should().Be(ResultStatus.Closed);
    }

    [Fact]
    public void OpenWindow_IsOpen_EvenWithFigures()
    {
        ResultStatus.WindowState(WindowEndsAt, WindowEndsAt.AddDays(-1), WindowEndsAt.AddDays(-1)).Should().Be(ResultStatus.Open);
    }

    [Fact]
    public void NoSendTime_IsNotStarted()
    {
        ResultStatus.WindowState(null, null, WindowEndsAt).Should().Be(ResultStatus.NotStarted);
    }
}
