namespace Parley.Campaigns.Features.Campaigns.Results;

/// <summary>
/// The result of a campaign as the product shows it. Every number and the verdict are computed on the server; the client
/// only displays them. Null statistics are undefined by the data.
/// </summary>
/// <param name="CampaignId">The campaign.</param>
/// <param name="Status">Campaign status.</param>
/// <param name="Source">Who created the campaign: <c>user</c> or <c>demo_seed</c>.</param>
/// <param name="WindowState"><c>not_started</c>, <c>open</c>, <c>awaiting_final</c> (the window has ended but the figures were not yet computed after its end, so they are still provisional) or <c>closed</c> (final).</param>
/// <param name="IsFinal">True only when the figures were computed at or after the window end. Before that every figure is provisional and no verdict is claimed.</param>
/// <param name="StartedAt">Send time of both arms, UTC.</param>
/// <param name="CompletedAt">Completion instant, UTC.</param>
/// <param name="AttributionWindowDays">Length of the attribution window.</param>
/// <param name="WindowEndsAt">Completion plus the window length: the instant the result becomes final.</param>
/// <param name="ComputedAt">When the attribution job last computed the figures; null when it never ran.</param>
/// <param name="Simulated">True when the window holds simulated orders. The UI labels the campaign as a demo with a simulated effect.</param>
/// <param name="Verdict"><c>measurable_lift</c>, <c>no_measurable_effect</c>, <c>unreliable</c> (final, but the sample ratio alarms, so no conclusion is drawn) or <c>too_early</c> (not final yet).</param>
/// <param name="SrmWarning">True when the sample ratio check alarms: a final result is then labeled <c>unreliable</c>.</param>
/// <param name="HoldoutShareBps">Share of the audience in the holdout.</param>
/// <param name="Treatment">The treatment arm.</param>
/// <param name="Holdout">The holdout arm.</param>
/// <param name="Lift">Treatment rate minus holdout rate.</param>
/// <param name="LiftCiLow">Lower bound of the 95 % interval of the lift.</param>
/// <param name="LiftCiHigh">Upper bound of the 95 % interval of the lift.</param>
/// <param name="PValue">Two-sided p-value of the pooled z-test.</param>
/// <param name="SrmPValue">Sample ratio mismatch p-value.</param>
/// <param name="MinimumDetectableEffect">Smallest lift this sample can detect (4.9 percentage points in the reference example).</param>
/// <param name="IncrementalRevenue">Incremental revenue of the treatment arm.</param>
public sealed record CampaignResultsView(
    Guid CampaignId,
    string Status,
    string Source,
    string WindowState,
    bool IsFinal,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int AttributionWindowDays,
    DateTime? WindowEndsAt,
    DateTime? ComputedAt,
    bool Simulated,
    string Verdict,
    bool SrmWarning,
    int HoldoutShareBps,
    CampaignResultsArmView Treatment,
    CampaignResultsArmView Holdout,
    double? Lift,
    double? LiftCiLow,
    double? LiftCiHigh,
    double? PValue,
    double? SrmPValue,
    double? MinimumDetectableEffect,
    double? IncrementalRevenue);
