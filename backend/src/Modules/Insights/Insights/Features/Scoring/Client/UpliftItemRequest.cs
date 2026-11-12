using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>One item of an uplift request: the contact as a UUID and its features.</summary>
public sealed record UpliftItemRequest(
    [property: JsonPropertyName("subject_id")] string SubjectId,
    [property: JsonPropertyName("features")] UpliftFeatureValues Features);
