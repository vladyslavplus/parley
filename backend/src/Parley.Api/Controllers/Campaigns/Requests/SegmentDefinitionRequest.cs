using Parley.Campaigns.Domain.Segments;

namespace Parley.Api.Controllers.Campaigns.Requests;

/// <summary>Filter definition of a segment as sent by the client. Missing lists mean open filters.</summary>
/// <param name="OwnerUserIds">Owners whose contacts match.</param>
/// <param name="CountryCodes">ISO alpha-2 country codes whose contacts match.</param>
/// <param name="TagIds">Tags a contact must carry.</param>
/// <param name="RfmSegments">RFM segment names.</param>
/// <param name="DaysSinceLastOrder">Recency filter in days.</param>
public sealed record SegmentDefinitionRequest(
    IReadOnlyList<Guid>? OwnerUserIds,
    IReadOnlyList<string>? CountryCodes,
    IReadOnlyList<Guid>? TagIds,
    IReadOnlyList<string>? RfmSegments,
    int? DaysSinceLastOrder)
{
    public SegmentDefinition ToDefinition() => new()
    {
        OwnerUserIds = OwnerUserIds?.ToList() ?? [],
        CountryCodes = CountryCodes?.ToList() ?? [],
        TagIds = TagIds?.ToList() ?? [],
        RfmSegments = RfmSegments?.ToList() ?? [],
        DaysSinceLastOrder = DaysSinceLastOrder,
    };
}
