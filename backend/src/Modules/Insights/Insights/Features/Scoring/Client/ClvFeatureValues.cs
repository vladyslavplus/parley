using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>Features of one contact for the CLV model (BG/NBD conventions, in days).</summary>
public sealed record ClvFeatureValues(
    [property: JsonPropertyName("frequency")] int Frequency,
    [property: JsonPropertyName("recency_days_since_first")] int RecencyDaysSinceFirst,
    [property: JsonPropertyName("tenure_days")] int TenureDays,
    [property: JsonPropertyName("monetary_value")] decimal MonetaryValue);
