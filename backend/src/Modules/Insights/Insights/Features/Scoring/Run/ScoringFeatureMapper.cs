using Parley.Crm.Contracts.Metrics;
using Parley.Insights.Features.Scoring.Client;

namespace Parley.Insights.Features.Scoring.Run;

/// <summary>
/// Maps the CRM customer metrics of a contact to the features of each model, in the contract's units. Pure functions: the
/// same metrics and the same as-of instant always give the same features. A contact without an order has no features and
/// is not sent to the service; its scores stay absent, the same as for RFM.
/// </summary>
public static class ScoringFeatureMapper
{
    /// <summary>True when the contact has at least one order, the precondition of both models.</summary>
    public static bool HasOrders(CrmCustomerMetricsRow row) => row.FirstOrderAt is not null && row.LastOrderAt is not null;

    /// <summary>Uplift features: recency and tenure from the as-of instant, the order windows and the open-deal flag.</summary>
    public static UpliftFeatureValues Uplift(CrmCustomerMetricsRow row, DateTimeOffset asOf)
    {
        var first = row.FirstOrderAt!.Value;
        var last = row.LastOrderAt!.Value;
        return new UpliftFeatureValues(
            RecencyDays: Days(asOf - last),
            TenureDays: Days(asOf - first),
            Orders90Days: row.Orders90Days,
            Orders365Days: row.Orders365Days,
            OrderCount: row.OrderCount,
            Value365Days: row.Value365Days,
            AvgOrderValue: row.AvgOrderValue,
            HasOpenDeal: row.HasOpenDeal ? 1 : 0);
    }

    /// <summary>
    /// CLV features in the BG/NBD convention: frequency is the repeat purchase days, recency is measured from the first order
    /// to the last, tenure from the first order to the as-of instant. The monetary value is the average repeat order value,
    /// which is zero when the contact has no repeat purchase.
    /// </summary>
    public static ClvFeatureValues Clv(CrmCustomerMetricsRow row, DateTimeOffset asOf)
    {
        var first = row.FirstOrderAt!.Value;
        var last = row.LastOrderAt!.Value;
        var frequency = Math.Max(row.PurchaseDayCount - 1, 0);
        return new ClvFeatureValues(
            Frequency: frequency,
            RecencyDaysSinceFirst: Days(last - first),
            TenureDays: Days(asOf - first),
            MonetaryValue: frequency == 0 ? 0m : row.AvgRepeatOrderValue);
    }

    /// <summary>Whole days of a span, never negative: a value dated after the as-of instant counts as zero.</summary>
    private static int Days(TimeSpan span) =>
        span <= TimeSpan.Zero ? 0 : (int)Math.Min(int.MaxValue, Math.Floor(span.TotalDays));
}
