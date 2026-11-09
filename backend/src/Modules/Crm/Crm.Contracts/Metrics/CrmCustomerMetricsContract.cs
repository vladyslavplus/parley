namespace Parley.Crm.Contracts.Metrics;

/// <summary>
/// Frozen definitions of the customer metrics. Used by the RFM rule, the CLV features and the scoring
/// jobs; a change needs a new feature schema version on the consumer side.
/// </summary>
public static class CrmCustomerMetricsContract
{
    /// <summary>Largest batch one read returns.</summary>
    public const int MaximumBatchSize = 5000;

    /// <summary>Short purchase window in days, ending at the as-of instant (inclusive), left open.</summary>
    public const int ShortWindowDays = 90;

    /// <summary>Long purchase window in days, ending at the as-of instant (inclusive), left open.</summary>
    public const int LongWindowDays = 365;
}
