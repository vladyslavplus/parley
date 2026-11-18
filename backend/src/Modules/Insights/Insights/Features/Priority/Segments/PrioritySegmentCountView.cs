namespace Parley.Insights.Features.Priority.Segments;

/// <summary>One segment of one taxonomy: how many of its contacts are on the day's list, and how many exist in the caller's scope.</summary>
/// <param name="Segment">Segment code.</param>
/// <param name="OnList">Contacts of the segment on the latest priority list, in the caller's scope.</param>
/// <param name="Total">Contacts of the segment in the caller's scope, from the day the taxonomy uses.</param>
public sealed record PrioritySegmentCountView(string Segment, int OnList, int Total);
