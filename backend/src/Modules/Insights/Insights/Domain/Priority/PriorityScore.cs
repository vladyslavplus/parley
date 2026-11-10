namespace Parley.Insights.Domain.Priority;

/// <summary>The priority of one contact in the RFM mode.</summary>
/// <param name="Value">Segment weight plus the bonuses; higher ranks first.</param>
/// <param name="ReasonCodes">The reasons, in the order they were found.</param>
/// <param name="SuggestedAction">The next action code.</param>
public sealed record PriorityScore(decimal Value, IReadOnlyList<string> ReasonCodes, string SuggestedAction);
