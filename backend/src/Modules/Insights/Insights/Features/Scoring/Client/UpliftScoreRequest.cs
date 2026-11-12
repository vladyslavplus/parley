using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>Body of <c>POST /score/uplift</c>. A null model version lets the service choose its default.</summary>
public sealed record UpliftScoreRequest(
    [property: JsonPropertyName("feature_schema_version")] int FeatureSchemaVersion,
    [property: JsonPropertyName("items")] IReadOnlyList<UpliftItemRequest> Items,
    [property: JsonPropertyName("model_version")] string? ModelVersion);
