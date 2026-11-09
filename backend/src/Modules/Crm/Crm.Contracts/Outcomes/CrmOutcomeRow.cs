namespace Parley.Crm.Contracts.Outcomes;

/// <summary>
/// What one contact did inside an outcome window. Identifiers and numbers only.
/// </summary>
/// <param name="ContactId">The contact.</param>
/// <param name="PurchaseCount">Non-cancellation orders inside the window.</param>
/// <param name="Revenue">Net value of all orders inside the window (cancellations subtract).</param>
/// <param name="FirstPurchaseAt">Earliest non-cancellation order inside the window, UTC; null without one.</param>
/// <param name="FirstWonDealClosedAt">Earliest won deal closed inside the window, UTC; null without one.</param>
public sealed record CrmOutcomeRow(
    Guid ContactId,
    int PurchaseCount,
    decimal Revenue,
    DateTimeOffset? FirstPurchaseAt,
    DateTimeOffset? FirstWonDealClosedAt);
