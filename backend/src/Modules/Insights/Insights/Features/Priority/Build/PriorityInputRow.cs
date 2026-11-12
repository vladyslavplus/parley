namespace Parley.Insights.Features.Priority.Build;

/// <summary>The snapshot values the priority rules read, one row per contact of the build day.</summary>
/// <param name="ContactId">The contact.</param>
/// <param name="OwnerUserId">Owner of the contact; null when unowned.</param>
/// <param name="Segment">RFM segment code.</param>
/// <param name="RTier">Recency tier; null without orders.</param>
/// <param name="FTier">Frequency tier; null without orders.</param>
/// <param name="MTier">Monetary tier; null without orders.</param>
public sealed record PriorityInputRow(
    Guid ContactId,
    Guid? OwnerUserId,
    string Segment,
    int? RTier,
    int? FTier,
    int? MTier);
