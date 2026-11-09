using Parley.Crm.Contracts.Audience;

namespace Parley.Campaigns.Domain.Segments;

/// <summary>
/// The one predicate that decides whether an audience row belongs to a segment. The segment preview counts with it and
/// the campaign audience freeze uses the same rule, so both agree on the count. Owner, country, tags and recency are
/// evaluated here; the RFM filter needs insights data and is reported by the callers before any count runs.
/// </summary>
public static class SegmentAudienceRules
{
    /// <summary>
    /// True when <paramref name="row"/> passes every filter of <paramref name="definition"/>. Recency is measured
    /// from <paramref name="asOfUtc"/>.
    /// </summary>
    public static bool Matches(CrmAudienceRow row, SegmentDefinition definition, DateTime asOfUtc)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(definition);

        return OwnerMatches(row, definition)
            && CountryMatches(row, definition)
            && TagsMatch(row, definition)
            && RecencyMatches(row, definition, asOfUtc);
    }

    /// <summary>True when the definition sets an RFM filter, which needs insights data.</summary>
    public static bool NeedsRfm(SegmentDefinition definition) => definition.RfmSegments.Count > 0;

    private static bool OwnerMatches(CrmAudienceRow row, SegmentDefinition definition) =>
        definition.OwnerUserIds.Count == 0
        || (row.OwnerUserId is { } owner && definition.OwnerUserIds.Contains(owner));

    private static bool CountryMatches(CrmAudienceRow row, SegmentDefinition definition) =>
        definition.CountryCodes.Count == 0
        || (row.CountryIso2 is { } country && definition.CountryCodes.Contains(country, StringComparer.OrdinalIgnoreCase));

    /// <summary>A contact must carry every listed tag.</summary>
    private static bool TagsMatch(CrmAudienceRow row, SegmentDefinition definition)
    {
        if (definition.TagIds.Count == 0)
        {
            return true;
        }

        var contactTags = row.TagIds ?? [];
        return definition.TagIds.All(tag => contactTags.Contains(tag));
    }

    /// <summary>A contact's last order must fall within the last <see cref="SegmentDefinition.DaysSinceLastOrder"/> days.</summary>
    private static bool RecencyMatches(CrmAudienceRow row, SegmentDefinition definition, DateTime asOfUtc)
    {
        if (definition.DaysSinceLastOrder is not { } days)
        {
            return true;
        }

        return row.LastOrderAt is { } lastOrder && lastOrder >= asOfUtc.AddDays(-days);
    }
}
