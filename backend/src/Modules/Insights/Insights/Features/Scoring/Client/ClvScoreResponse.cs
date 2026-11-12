using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>Response of <c>POST /score/clv</c>.</summary>
public sealed record ClvScoreResponse(
    [property: JsonPropertyName("model_name")] string ModelName,
    [property: JsonPropertyName("model_version")] string ModelVersion,
    [property: JsonPropertyName("feature_schema_version")] int FeatureSchemaVersion,
    [property: JsonPropertyName("provenance")] string Provenance,
    [property: JsonPropertyName("scores")] IReadOnlyList<ClvScoreItemResponse> Scores,
    [property: JsonPropertyName("rejected")] IReadOnlyList<ScoringRejectionResponse> Rejected);
