namespace Parley.Insights.Domain.Priority;

/// <summary>Scoring modes of the priority list.</summary>
public static class PriorityModes
{
    /// <summary>Priority from the RFM segment and the CRM facts. The only mode until the ML scores exist.</summary>
    public const string Rfm = "rfm";

    /// <summary>Priority from the uplift scores and the RFM segment. Used when the day's ML scores are usable.</summary>
    public const string Ml = "ml";
}
