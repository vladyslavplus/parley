namespace Parley.Campaigns.Domain.Lift;

/// <summary>
/// The statistics of one comparison. Every value that the data cannot define (a zero denominator) is null, never a number
/// that looks valid.
/// </summary>
/// <param name="RateTreatment">Conversions over recipients in the treatment arm.</param>
/// <param name="RateHoldout">Conversions over recipients in the holdout arm.</param>
/// <param name="Lift">Treatment rate minus holdout rate.</param>
/// <param name="CiLow">Lower bound of the Newcombe-Wilson interval of the lift.</param>
/// <param name="CiHigh">Upper bound of the Newcombe-Wilson interval of the lift.</param>
/// <param name="ZTestPValue">Two-sided p-value of the pooled two-proportion z-test.</param>
/// <param name="SrmPValue">Sample ratio mismatch p-value against the configured share.</param>
/// <param name="MinimumDetectableEffect">Smallest absolute lift the counts can detect at the configured alpha and power.</param>
/// <param name="IncrementalRevenue">(Mean revenue treatment minus mean revenue holdout) times the treatment count.</param>
public sealed record LiftResult(
    double? RateTreatment,
    double? RateHoldout,
    double? Lift,
    double? CiLow,
    double? CiHigh,
    double? ZTestPValue,
    double? SrmPValue,
    double? MinimumDetectableEffect,
    double? IncrementalRevenue);
