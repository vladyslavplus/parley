using Parley.Campaigns.Domain.Segments;

namespace Parley.Campaigns.Features.Segments.Shared;

/// <summary>Brings a definition to its stored form: trimmed, upper-case country codes, no duplicates.</summary>
internal static class SegmentDefinitionRules
{
    public static SegmentDefinition Normalize(SegmentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return new SegmentDefinition
        {
            OwnerUserIds = definition.OwnerUserIds.Distinct().ToList(),
            CountryCodes = definition.CountryCodes.Select(code => code.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToList(),
            TagIds = definition.TagIds.Distinct().ToList(),
            RfmSegments = definition.RfmSegments.Select(segment => segment.Trim()).Distinct(StringComparer.Ordinal).ToList(),
            DaysSinceLastOrder = definition.DaysSinceLastOrder,
        };
    }
}
