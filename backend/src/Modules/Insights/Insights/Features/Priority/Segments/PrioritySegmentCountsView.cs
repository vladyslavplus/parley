namespace Parley.Insights.Features.Priority.Segments;

/// <summary>
/// Counts of both segment taxonomies for the latest completed priority day, in the caller's scope. <c>asOf</c> is the day of
/// the list; <c>rfmAsOf</c> and <c>upliftAsOf</c> name the days the totals come from, so a caller sees when they differ. Every
/// segment is listed, even with zero contacts.
/// </summary>
/// <param name="AsOf">Day of the priority list; null before the first complete day.</param>
/// <param name="RfmAsOf">Day of the latest complete RFM snapshot the RFM totals come from; null when there is none.</param>
/// <param name="UpliftAsOf">Day of the latest uplift scores the uplift totals come from; null when there are none.</param>
/// <param name="Rfm">RFM segments, in the documented segment order.</param>
/// <param name="Uplift">Uplift segments, in the order the scoring rules decide them.</param>
public sealed record PrioritySegmentCountsView(
    DateOnly? AsOf,
    DateOnly? RfmAsOf,
    DateOnly? UpliftAsOf,
    IReadOnlyList<PrioritySegmentCountView> Rfm,
    IReadOnlyList<PrioritySegmentCountView> Uplift);
