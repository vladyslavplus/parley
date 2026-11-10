namespace Parley.Insights.Domain.Priority;

/// <summary>One contact that passed the priority rules, before it is ranked among its owner's contacts.</summary>
/// <param name="ContactId">The contact.</param>
/// <param name="OwnerUserId">Owner of the contact; null when unowned.</param>
/// <param name="Segment">RFM segment of the contact on the build day.</param>
/// <param name="Value">Priority value; higher ranks first.</param>
/// <param name="ReasonCodes">Reason codes that explain the value.</param>
/// <param name="SuggestedAction">Next action code.</param>
public sealed record PriorityCandidate(
    Guid ContactId,
    Guid? OwnerUserId,
    string Segment,
    decimal Value,
    IReadOnlyList<string> ReasonCodes,
    string SuggestedAction);
