using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>One uplift score of the response.</summary>
public sealed record UpliftScoreItemResponse(
    [property: JsonPropertyName("subject_id")] string SubjectId,
    [property: JsonPropertyName("uplift")] decimal Uplift,
    [property: JsonPropertyName("p_treated")] decimal PTreated,
    [property: JsonPropertyName("p_control")] decimal PControl,
    [property: JsonPropertyName("segment")] string Segment);
