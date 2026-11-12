using System.Text.Json.Serialization;

namespace Parley.Insights.Features.Scoring.Client;

/// <summary>Features of one contact for the uplift model, in the contract's units (days and money).</summary>
public sealed record UpliftFeatureValues(
    [property: JsonPropertyName("recency_days")] int RecencyDays,
    [property: JsonPropertyName("tenure_days")] int TenureDays,
    [property: JsonPropertyName("orders_90d")] int Orders90Days,
    [property: JsonPropertyName("orders_365d")] int Orders365Days,
    [property: JsonPropertyName("order_count")] int OrderCount,
    [property: JsonPropertyName("value_365d")] decimal Value365Days,
    [property: JsonPropertyName("avg_order_value")] decimal AvgOrderValue,
    [property: JsonPropertyName("has_open_deal")] int HasOpenDeal);
