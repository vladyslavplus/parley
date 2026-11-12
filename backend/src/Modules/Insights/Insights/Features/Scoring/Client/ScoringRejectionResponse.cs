using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>One item the service refused. The subject id is as sent, or null when it was not a string.</summary>
public sealed record ScoringRejectionResponse(
    [property: JsonPropertyName("subject_id")] string? SubjectId,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("detail")] string Detail);
