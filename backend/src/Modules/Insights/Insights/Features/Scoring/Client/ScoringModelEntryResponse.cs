using System.Text.Json;
using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>
/// One model of the registry, as the service reports it. <c>segment_rules</c> carries the uplift segment thresholds of the
/// manifest; it is absent or null for other models and for a service that does not publish it.
/// </summary>
public sealed record ScoringModelEntryResponse(
    [property: JsonPropertyName("model_name")] string ModelName,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("feature_schema_version")] int FeatureSchemaVersion,
    [property: JsonPropertyName("provenance")] string Provenance,
    [property: JsonPropertyName("trained_at")] string TrainedAt,
    [property: JsonPropertyName("metrics")] JsonElement Metrics,
    [property: JsonPropertyName("segment_rules")] JsonElement? SegmentRules,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("error_code")] string? ErrorCode);
