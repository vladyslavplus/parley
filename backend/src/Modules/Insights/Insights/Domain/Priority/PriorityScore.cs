namespace Parley.Insights.Domain.Priority;

/// <summary>The priority of one contact in its scoring mode.</summary>
/// <param name="Value">Higher ranks first: the RFM value or, in the ML mode, the uplift value on the same scale. A do-not-disturb caution carries zero.</param>
/// <param name="ReasonCodes">The reasons, in the order they were found.</param>
/// <param name="SuggestedAction">The next action code.</param>
public sealed record PriorityScore(decimal Value, IReadOnlyList<string> ReasonCodes, string SuggestedAction);
