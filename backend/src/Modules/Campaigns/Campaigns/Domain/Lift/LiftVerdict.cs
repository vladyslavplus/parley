namespace Parley.Campaigns.Domain.Lift;

/// <summary>
/// The one-line answer the product shows for a campaign. Computed on the server, so no client recomputes statistics.
/// Wording rules: nothing is claimed until the result is final, a sample ratio alarm makes the result unreliable, and a lift
/// is claimed only when the result is final, the sample ratio is sound and the interval excludes zero. The verdict reports
/// what the randomized comparison shows; it does not claim a cause beyond that comparison.
/// </summary>
public static class LiftVerdict
{
    /// <summary>The result is not final yet: the window is open, or its final figures are still to be computed.</summary>
    public const string TooEarly = "too_early";

    /// <summary>The result is final and the data does not separate the arms.</summary>
    public const string NoMeasurableEffect = "no_measurable_effect";

    /// <summary>The result is final, the sample ratio is sound and the interval of the lift excludes zero.</summary>
    public const string MeasurableLift = "measurable_lift";

    /// <summary>
    /// The result is final but the sample ratio alarms, so the assignment is suspect and no conclusion is drawn from the
    /// comparison, whatever the interval says.
    /// </summary>
    public const string Unreliable = "unreliable";

    /// <param name="isFinal">The figures were computed at or after the window end (see <see cref="ResultStatus.IsFinal"/>).</param>
    /// <param name="srmPValue">Sample ratio mismatch p-value; null when undefined.</param>
    /// <param name="srmAlarmP">Threshold below which the sample ratio alarms.</param>
    /// <param name="ciLow">Lower bound of the interval of the lift; null when undefined.</param>
    /// <param name="ciHigh">Upper bound of the interval of the lift; null when undefined.</param>
    public static string Decide(bool isFinal, double? srmPValue, double srmAlarmP, double? ciLow, double? ciHigh)
    {
        if (!isFinal)
        {
            return TooEarly;
        }

        if (srmPValue is { } srm && srm < srmAlarmP)
        {
            return Unreliable;
        }

        return ciLow is { } low && ciHigh is { } high && (low > 0d || high < 0d)
            ? MeasurableLift
            : NoMeasurableEffect;
    }
}
