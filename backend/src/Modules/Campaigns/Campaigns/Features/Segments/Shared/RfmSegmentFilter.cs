using Parley.Campaigns.Domain.Segments;
using Parley.Crm.Contracts.Audience;
using Parley.Insights.Contracts.Scores;

namespace Parley.Campaigns.Features.Segments.Shared;

/// <summary>
/// Applies the RFM segment filter of a definition to rows that already pass the other filters. The segments come from the
/// score reader; a contact without tiers (no orders) is not in any segment, so it never matches an RFM filter.
/// </summary>
public static class RfmSegmentFilter
{
    /// <summary>
    /// Filters the rows by segment on <paramref name="rfmDay"/>. The caller resolves the day once per audience and passes the
    /// same value to every batch, so the batches of one audience never mix two RFM days.
    /// </summary>
    public static async Task<IReadOnlyList<CrmAudienceRow>> ApplyAsync(
        IReadOnlyList<CrmAudienceRow> rows,
        SegmentDefinition definition,
        IScoreReader scores,
        DateOnly? rfmDay,
        CancellationToken cancellationToken)
    {
        if (!SegmentAudienceRules.NeedsRfm(definition) || rows.Count == 0)
        {
            return rows;
        }

        var segments = await scores.ReadRfmSegmentsAsync(rows.Select(row => row.ContactId).ToList(), rfmDay, cancellationToken);
        return rows
            .Where(row => segments.TryGetValue(row.ContactId, out var segment) && definition.RfmSegments.Contains(segment, StringComparer.Ordinal))
            .ToList();
    }
}
