namespace Parley.Insights.Contracts.DemoSeed;

/// <summary>What the demo insights step built. ML is best effort: a missing service leaves the workspace in RFM mode.</summary>
/// <param name="RfmBuilt">True when the RFM snapshot of the day completed.</param>
/// <param name="PriorityBuilt">True when the priority list of the day completed.</param>
/// <param name="MlEnabled">True when an uplift model was found, ML was switched on and its scoring run completed.</param>
/// <param name="MlFailure">Why the ML part was skipped, if it was; null on success.</param>
public sealed record DemoInsightsReport(bool RfmBuilt, bool PriorityBuilt, bool MlEnabled, string? MlFailure);
