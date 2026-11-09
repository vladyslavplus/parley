using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Campaigns.Domain.Segments;

/// <summary>A named audience definition. The definition is evaluated when a campaign freezes its audience.</summary>
public sealed class Segment : BusinessEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>The typed filter tree, stored as jsonb.</summary>
    public SegmentDefinition Definition { get; set; } = new();

    /// <summary>Set when the segment is archived; archived segments stay referenced by past campaigns.</summary>
    public DateTime? ArchivedAt { get; set; }
}
