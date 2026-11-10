namespace Parley.Campaigns.Features.Segments.Shared;

/// <summary>
/// Size of the audience a definition selects, and a sample of contact identifiers. The preview carries no contact
/// names or addresses.
/// </summary>
/// <param name="Count">Number of eligible contacts the definition matches.</param>
/// <param name="Sample">Identifiers of the first matching contacts, in contact id order.</param>
public sealed record SegmentPreviewView(int Count, IReadOnlyList<Guid> Sample);
