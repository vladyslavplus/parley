namespace Parley.Insights.Features.Rfm.Compute;

/// <summary>One snapshot row as it travels to the database: the JSON array of a batch carries these, one per contact.</summary>
/// <param name="Id">Identifier of a new row; an existing row keeps its own.</param>
/// <param name="ContactId">The contact.</param>
/// <param name="OwnerUserId">Owner of the contact at the snapshot; null when unowned.</param>
/// <param name="RecencyDays">Days since the last order; null without orders.</param>
/// <param name="Frequency">Orders in the last 365 days.</param>
/// <param name="Monetary">Value in the last 365 days.</param>
/// <param name="RTier">Recency tier; null without orders.</param>
/// <param name="FTier">Frequency tier; null without orders.</param>
/// <param name="MTier">Monetary tier; null without orders.</param>
/// <param name="Segment">Segment code.</param>
public sealed record RfmSnapshotRow(
    Guid Id,
    Guid ContactId,
    Guid? OwnerUserId,
    int? RecencyDays,
    int Frequency,
    decimal Monetary,
    int? RTier,
    int? FTier,
    int? MTier,
    string Segment);
