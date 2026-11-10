namespace Parley.Crm.Contracts.Outcomes;

/// <summary>
/// What one contact did inside an outcome window. Identifiers and numbers only.
/// </summary>
/// <param name="ContactId">The contact.</param>
/// <param name="PurchaseCount">Non-cancellation orders inside the window.</param>
/// <param name="Revenue">
/// Net value of all orders inside the window (cancellations subtract), summed in each order's stored currency with no
/// conversion. Callers that add revenue across rows must know the organization's orders share one currency.
/// </param>
/// <param name="FirstPurchaseAt">Earliest non-cancellation order inside the window, UTC; null without one.</param>
/// <param name="FirstWonDealClosedAt">Earliest won deal closed inside the window, UTC; null without one.</param>
/// <param name="SimulatedPurchaseCount">Purchases inside the window that the demo seed simulated. Zero for real customers.</param>
public sealed record CrmOutcomeRow(
    Guid ContactId,
    int PurchaseCount,
    decimal Revenue,
    DateTimeOffset? FirstPurchaseAt,
    DateTimeOffset? FirstWonDealClosedAt,
    int SimulatedPurchaseCount = 0);
