namespace Parley.Insights.Domain.Ml;

/// <summary>Keys of the typed score payload the scoring runs store, shared by every reader of <c>scores.payload</c>.</summary>
public static class ScorePayloadKeys
{
    public const string Segment = "segment";

    public const string PTreated = "p_treated";

    public const string PControl = "p_control";

    public const string PAlive = "p_alive";

    public const string ExpectedPurchases = "expected_purchases";

    public const string ExpectedAverageValue = "expected_avg_value";

    public const string HorizonDays = "horizon_days";
}
