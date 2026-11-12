namespace Parley.Campaigns.Contracts.Results;

/// <summary>
/// The lift summary of a campaign for the dashboard: the verdict decided by the campaigns module, the simulated flag, the
/// source and the lift with its interval. No counts and no rates: the dashboard shows the verdict, it never recomputes a
/// statistic. The verdict follows the same final-only rule as the results endpoint.
/// </summary>
/// <param name="CampaignId">The campaign.</param>
/// <param name="Name">Display name of the campaign.</param>
/// <param name="Source">Who created the campaign: <c>user</c> or <c>demo_seed</c>. A demo campaign is labeled as such.</param>
/// <param name="Verdict">One of <c>measurable_lift</c>, <c>no_measurable_effect</c>, <c>unreliable</c>, <c>too_early</c>.</param>
/// <param name="Simulated">True when the campaign's result includes simulated demo orders.</param>
/// <param name="WindowState"><c>open</c>, <c>awaiting_final</c> or <c>closed</c> (final).</param>
/// <param name="IsFinal">True only when the figures behind the verdict are final.</param>
/// <param name="CompletedAt">Completion instant of the campaign, UTC.</param>
/// <param name="Lift">Lift of the treatment over the holdout; null when undefined.</param>
/// <param name="LiftCiLow">Lower bound of the 95 % interval of the lift.</param>
/// <param name="LiftCiHigh">Upper bound of the 95 % interval of the lift.</param>
public sealed record CampaignResultSummary(
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
