using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>One CLV score of the response.</summary>
public sealed record ClvScoreItemResponse(
    [property: JsonPropertyName("subject_id")] string SubjectId,
    [property: JsonPropertyName("clv")] decimal Clv,
    [property: JsonPropertyName("p_alive")] decimal PAlive,
    [property: JsonPropertyName("expected_purchases")] decimal ExpectedPurchases,
    [property: JsonPropertyName("expected_avg_value")] decimal ExpectedAvgValue,
    [property: JsonPropertyName("horizon_days")] int HorizonDays);
