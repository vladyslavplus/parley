namespace Parley.Insights.Features.Scoring.Shared;

/// <summary>The version each kind scores with right now, or null when the registry holds no version of the kind.</summary>
/// <param name="Uplift">Effective uplift version.</param>
/// <param name="Clv">Effective CLV version.</param>
public sealed record EffectiveModelVersions(string? Uplift, string? Clv);
