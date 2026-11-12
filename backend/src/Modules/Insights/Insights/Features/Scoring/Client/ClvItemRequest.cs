using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>One item of a CLV request.</summary>
public sealed record ClvItemRequest(
    [property: JsonPropertyName("subject_id")] string SubjectId,
    [property: JsonPropertyName("features")] ClvFeatureValues Features);
