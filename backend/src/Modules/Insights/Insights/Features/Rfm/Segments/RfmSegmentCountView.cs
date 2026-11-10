namespace Parley.Insights.Features.Rfm.Segments;

/// <summary>One RFM segment and the number of contacts in it, within the caller's scope.</summary>
/// <param name="Segment">Segment code.</param>
/// <param name="Count">Contacts in the segment.</param>
public sealed record RfmSegmentCountView(string Segment, int Count);
