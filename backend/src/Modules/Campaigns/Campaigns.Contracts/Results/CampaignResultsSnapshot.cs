namespace Parley.Campaigns.Contracts.Results;

/// <summary>
/// The stored result of one campaign with its window state, as of the read. Identifiers, counts, rates and statistics only.
/// Null statistics are undefined: the data cannot define them yet.
/// </summary>
/// <param name="CampaignId">The campaign.</param>
/// <param name="Status">Campaign status (domain model 7.5).</param>
/// <param name="Source">Who created the campaign: <c>user</c> or <c>demo_seed</c>.</param>
/// <param name="StartedAt">Send time of both arms, UTC; null before the campaign started.</param>
/// <param name="CompletedAt">Completion instant, UTC; null before completion.</param>
/// <param name="AttributionWindowDays">Length of the attribution window after the send time.</param>
/// <param name="WindowEndsAt">Send time plus the window; null before the campaign started.</param>
/// <param name="WindowState"><c>not_started</c>, <c>open</c>, <c>awaiting_final</c> (window ended, figures not yet computed after it) or <c>closed</c> (final).</param>
/// <param name="IsFinal">True only when the figures were computed at or after the window end. Only final figures support a claim.</param>
/// <param name="ComputedAt">When the attribution job last wrote the result; null when it never ran.</param>
/// <param name="Simulated">True when the window holds simulated orders (demo campaigns with an injected effect).</param>
/// <param name="HoldoutShareBps">Share of the audience in the holdout.</param>
/// <param name="TreatmentAssigned">Frozen recipients in the treatment arm, targeted or not.</param>
/// <param name="HoldoutAssigned">Frozen recipients in the holdout arm, targeted or not.</param>
/// <param name="TreatmentTargeted">Targeted recipients in the treatment arm.</param>
/// <param name="HoldoutTargeted">Targeted recipients in the holdout arm.</param>
/// <param name="TreatmentConverted">Converted targeted recipients in the treatment arm.</param>
/// <param name="HoldoutConverted">Converted targeted recipients in the holdout arm.</param>
/// <param name="TreatmentRevenue">Net revenue of the targeted treatment recipients in the window.</param>
/// <param name="HoldoutRevenue">Net revenue of the targeted holdout recipients in the window.</param>
/// <param name="RateTreatment">Conversion rate of the treatment arm.</param>
/// <param name="RateHoldout">Conversion rate of the holdout arm.</param>
/// <param name="Lift">Treatment rate minus holdout rate.</param>
/// <param name="LiftCiLow">Lower bound of the 95 % Newcombe-Wilson interval of the lift.</param>
/// <param name="LiftCiHigh">Upper bound of the 95 % Newcombe-Wilson interval of the lift.</param>
/// <param name="PValue">Two-sided pooled z-test p-value.</param>
/// <param name="SrmPValue">Sample ratio mismatch p-value over the assigned counts.</param>
/// <param name="MinimumDetectableEffect">Smallest lift the targeted counts can detect at the configured alpha and power.</param>
/// <param name="IncrementalRevenue">Incremental revenue of the treatment arm, per targeted treatment recipient times the count.</param>
public sealed record CampaignResultsSnapshot(
    Guid CampaignId,
    string Status,
    string Source,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int AttributionWindowDays,
    DateTime? WindowEndsAt,
    string WindowState,
    bool IsFinal,
    DateTime? ComputedAt,
    bool Simulated,
    int HoldoutShareBps,
    long TreatmentAssigned,
    long HoldoutAssigned,
    long TreatmentTargeted,
    long HoldoutTargeted,
    long TreatmentConverted,
    long HoldoutConverted,
    decimal TreatmentRevenue,
    decimal HoldoutRevenue,
    double? RateTreatment,
    double? RateHoldout,
    double? Lift,
    double? LiftCiLow,
    double? LiftCiHigh,
    double? PValue,
    double? SrmPValue,
    double? MinimumDetectableEffect,
    double? IncrementalRevenue);
