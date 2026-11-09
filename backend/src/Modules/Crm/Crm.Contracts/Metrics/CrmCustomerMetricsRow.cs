namespace Parley.Crm.Contracts.Metrics;

/// <summary>
/// Purchase features of one live contact as of an instant. Identifiers and numbers only; no personal data.
/// Money is in the organization currency. Orders after the as-of instant are ignored.
/// </summary>
/// <param name="ContactId">The contact.</param>
/// <param name="OwnerUserId">Owner of the contact; null when unowned.</param>
/// <param name="FirstOrderAt">Earliest non-cancellation order, UTC; null without purchases.</param>
/// <param name="LastOrderAt">Latest non-cancellation order, UTC; null without purchases.</param>
/// <param name="OrderCount">Non-cancellation orders. Cancellations are not counted here.</param>
/// <param name="PurchaseDayCount">Distinct UTC days with at least one non-cancellation order.</param>
/// <param name="Orders90Days">Non-cancellation orders in the short window.</param>
/// <param name="Orders365Days">Non-cancellation orders in the long window.</param>
/// <param name="TotalValue">Net value of all orders up to the as-of instant (cancellations subtract).</param>
/// <param name="Value365Days">Net value of all orders in the long window.</param>
/// <param name="AvgOrderValue">TotalValue divided by OrderCount, rounded to 4 places; 0 without purchases.</param>
/// <param name="AvgRepeatOrderValue">
/// Mean daily net spend over the purchase days after the first purchase day, rounded to 4 places (the Gamma-Gamma
/// monetary input); 0 without repeat purchase days. A daily net spend sums all orders of that UTC day.
/// </param>
/// <param name="HasOpenDeal">True when the contact has a live deal with status open.</param>
public sealed record CrmCustomerMetricsRow(
    Guid ContactId,
    Guid? OwnerUserId,
    DateTimeOffset? FirstOrderAt,
    DateTimeOffset? LastOrderAt,
    int OrderCount,
    int PurchaseDayCount,
    int Orders90Days,
    int Orders365Days,
    decimal TotalValue,
    decimal Value365Days,
    decimal AvgOrderValue,
    decimal AvgRepeatOrderValue,
    bool HasOpenDeal);
