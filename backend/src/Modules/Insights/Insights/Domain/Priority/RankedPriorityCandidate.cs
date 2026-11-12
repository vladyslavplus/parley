namespace Parley.Insights.Domain.Priority;

/// <summary>A candidate with its rank among the contacts of the same owner.</summary>
/// <param name="Candidate">The contact.</param>
/// <param name="Rank">1 for the owner's first contact; ranks run without gaps.</param>
public sealed record RankedPriorityCandidate(PriorityCandidate Candidate, int Rank);
