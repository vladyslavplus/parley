namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>One simulated order.</summary>
/// <param name="ContactId">The contact that receives the order.</param>
/// <param name="ExternalRef">Stable reference of the order, unique per organization and source.</param>
/// <param name="OrderedAt">Instant of the order, UTC.</param>
/// <param name="TotalAmount">Amount in the organization currency.</param>
public sealed record CrmSimulatedOrder(Guid ContactId, string ExternalRef, DateTimeOffset OrderedAt, decimal TotalAmount);
