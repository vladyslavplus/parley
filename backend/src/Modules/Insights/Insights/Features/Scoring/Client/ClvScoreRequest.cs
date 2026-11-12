using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>Body of <c>POST /score/clv</c>.</summary>
public sealed record ClvScoreRequest(
    [property: JsonPropertyName("horizon_days")] int HorizonDays,
    [property: JsonPropertyName("feature_schema_version")] int FeatureSchemaVersion,
    [property: JsonPropertyName("items")] IReadOnlyList<ClvItemRequest> Items,
    [property: JsonPropertyName("model_version")] string? ModelVersion);
