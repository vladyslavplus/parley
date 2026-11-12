using System.Text.Json;

namespace Parley.Insights.Features.Scoring.Models;

/// <summary>One registry entry. The metrics are the evaluation numbers of the model's manifest, as the service reports them.</summary>
public sealed record ScoringModelView(
    string ModelName,
    string Version,
    string Kind,
    int FeatureSchemaVersion,
    string Provenance,
    DateTime TrainedAt,
    DateTime SyncedAt,
    JsonElement Metrics,
    bool IsActive);
