namespace Parley.Campaigns.Features.Segments.Shared;

/// <summary>A segment row of the library list.</summary>
/// <param name="Id">Segment identifier.</param>
/// <param name="Name">Segment name.</param>
public sealed record SegmentSummaryView(Guid Id, string Name);
