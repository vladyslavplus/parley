namespace Parley.Insights.Features.Dashboard.Overview;

/// <summary>The lift summary of the latest completed campaign, as decided by the campaigns module.</summary>
/// <param name="CampaignId">The campaign.</param>
/// <param name="Name">Display name of the campaign.</param>
/// <param name="Source">Who created the campaign: <c>user</c> or <c>demo_seed</c>. The dashboard labels a demo campaign.</param>
/// <param name="Verdict">One of <c>measurable_lift</c>, <c>no_measurable_effect</c>, <c>unreliable</c>, <c>too_early</c>. Final-only, as on the results endpoint.</param>
/// <param name="Simulated">True when the result includes simulated demo orders.</param>
/// <param name="WindowState"><c>open</c>, <c>awaiting_final</c> or <c>closed</c> (final).</param>
/// <param name="IsFinal">True only when the figures behind the verdict are final.</param>
/// <param name="CompletedAt">Completion instant, UTC.</param>
/// <param name="Lift">Lift over the holdout; null when undefined.</param>
/// <param name="LiftCiLow">Lower bound of the interval of the lift.</param>
/// <param name="LiftCiHigh">Upper bound of the interval of the lift.</param>
public sealed record LatestCampaignView(
    Guid CampaignId,
    string Name,
    string Source,
    string Verdict,
    bool Simulated,
    string WindowState,
    bool IsFinal,
    DateTime? CompletedAt,
    double? Lift,
    double? LiftCiLow,
    double? LiftCiHigh);
