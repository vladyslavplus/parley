using System.Text.Json;

namespace Parley.Insights.Features.Scoring.Models;

/// <summary>
/// One registry entry. The metrics are the evaluation numbers of the model's manifest, as the service reports them.
/// <c>IsAvailable</c> is false for a row the last successful sync no longer saw: it stays as history and can never be used.
/// </summary>
public sealed record ScoringModelView(
    string ModelName,
    string Version,
    string Kind,
    int FeatureSchemaVersion,
    string Provenance,
    DateTime TrainedAt,
    DateTime SyncedAt,
    JsonElement Metrics,
    bool IsAvailable,
    bool IsActive);
