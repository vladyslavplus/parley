namespace Parley.Insights.Domain.Rfm;

/// <summary>The values of one contact that the tiers are computed from. Only contacts with at least one order take part.</summary>
/// <param name="ContactId">The contact.</param>
/// <param name="OwnerUserId">Owner of the contact, copied into the snapshot; null when there is none.</param>
/// <param name="RecencyDays">Whole days from the last order to the reading instant.</param>
/// <param name="Orders365Days">Orders in the last 365 days.</param>
/// <param name="Value365Days">Net value in the last 365 days.</param>
public sealed record RfmInput(Guid ContactId, Guid? OwnerUserId, int RecencyDays, int Orders365Days, decimal Value365Days);
