using System.Text.Json;
using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>One model of the registry, as the service reports it.</summary>
public sealed record ScoringModelEntryResponse(
    [property: JsonPropertyName("model_name")] string ModelName,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("feature_schema_version")] int FeatureSchemaVersion,
    [property: JsonPropertyName("provenance")] string Provenance,
    [property: JsonPropertyName("trained_at")] string TrainedAt,
    [property: JsonPropertyName("metrics")] JsonElement Metrics,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("error_code")] string? ErrorCode);
