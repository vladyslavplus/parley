namespace Parley.Insights.Contracts.Priority;

/// <summary>One contact of the priority list, with the codes that explain its rank.</summary>
/// <param name="ContactId">The contact.</param>
/// <param name="OwnerUserId">Owner of the contact; null when the contact has no owner.</param>
/// <param name="Rank">Position of the contact among the list of its owner, starting at 1.</param>
/// <param name="Value">Priority value; higher ranks first.</param>
/// <param name="Mode">Scoring mode that produced the value: <c>rfm</c> until the ML scores exist.</param>
/// <param name="Segment">RFM segment of the contact.</param>
/// <param name="ReasonCodes">Stable reason codes; the text is localized by the caller.</param>
/// <param name="SuggestedAction">Stable code of the next action.</param>
public sealed record PriorityEntry(
    Guid ContactId,
    Guid? OwnerUserId,
    int Rank,
    decimal Value,
    string Mode,
    string Segment,
    IReadOnlyList<string> ReasonCodes,
    string SuggestedAction);
