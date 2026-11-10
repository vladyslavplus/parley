namespace Parley.Campaigns.Domain.Segments;

/// <summary>
/// Typed filter tree of a segment. An empty list (or a null day count) leaves that filter open; a contact must pass
/// every filter that is set.
/// </summary>
public sealed class SegmentDefinition
{
    /// <summary>Owners whose contacts match. Empty means any owner, including none.</summary>
    public List<Guid> OwnerUserIds { get; set; } = [];

    /// <summary>ISO 3166-1 alpha-2 codes whose contacts match. Empty means any country.</summary>
    public List<string> CountryCodes { get; set; } = [];

    /// <summary>Tags a contact must carry; a contact must have every listed tag.</summary>
    public List<Guid> TagIds { get; set; } = [];

    /// <summary>RFM segment names. Not evaluated until the insights module provides RFM data; the preview reports this.</summary>
    public List<string> RfmSegments { get; set; } = [];

    /// <summary>Contacts whose last order is at most this many days before the evaluation instant.</summary>
    public int? DaysSinceLastOrder { get; set; }
}
