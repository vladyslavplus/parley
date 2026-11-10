namespace Parley.Insights.Features.Priority.List;

/// <summary>The stored priority item as the keyset read projects it, before the contact names are added.</summary>
public sealed record PriorityItemRow(
    Guid Id,
    Guid ContactId,
    Guid? OwnerUserId,
    int Rank,
    decimal Value,
    string Mode,
    string Segment,
    string[] ReasonCodes,
    string SuggestedAction);
