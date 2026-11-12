namespace Parley.Insights.Features.Scoring.Models;

/// <summary>The outcome of an admin refresh of the model registry.</summary>
/// <param name="Synced">Number of available models the scoring service reported and the registry now holds.</param>
public sealed record ScoringModelsSyncView(int Synced);
