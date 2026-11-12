using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>Response of <c>GET /models</c>.</summary>
public sealed record ScoringModelsResponse(
    [property: JsonPropertyName("models")] IReadOnlyList<ScoringModelEntryResponse> Models);
