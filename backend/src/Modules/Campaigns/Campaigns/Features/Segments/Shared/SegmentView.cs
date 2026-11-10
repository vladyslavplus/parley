using Parley.Campaigns.Domain.Segments;

namespace Parley.Campaigns.Features.Segments.Shared;

/// <summary>A segment with its filter definition.</summary>
/// <param name="Id">Segment identifier.</param>
/// <param name="Name">Name shown in the segment library.</param>
/// <param name="Definition">The typed filter tree.</param>
/// <param name="Version">Row version to send back on update; a changed row answers 409.</param>
public sealed record SegmentView(Guid Id, string Name, SegmentDefinition Definition, uint Version);
