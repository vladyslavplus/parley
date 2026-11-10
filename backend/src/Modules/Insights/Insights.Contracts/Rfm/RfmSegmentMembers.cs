namespace Parley.Insights.Contracts.Rfm;

/// <summary>
/// The contacts of one RFM segment, bounded by the caller's maximum.
/// </summary>
/// <param name="ContactIds">Contact identifiers, at most the maximum, in identifier order.</param>
/// <param name="Truncated">True when the segment has more contacts than the maximum; <paramref name="ContactIds"/> is then partial.</param>
public sealed record RfmSegmentMembers(IReadOnlyList<Guid> ContactIds, bool Truncated);
