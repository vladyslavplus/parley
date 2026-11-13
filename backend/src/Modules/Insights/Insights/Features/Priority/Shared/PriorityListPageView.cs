namespace Parley.Insights.Features.Priority.Shared;

/// <summary>
/// One keyset page of the priority list, with the mode the list was built in and the ML status that explains it. The
/// <c>items</c> and <c>nextCursor</c> keys are those of a keyset page; <c>mode</c> is <c>ml</c> or <c>rfm</c>, and
/// <c>mlStatus</c> is one of the values of the priority ML status. <c>modelProvenance</c> is the provenance of the uplift
/// model the list ranked with; null in the RFM mode and when no uplift model is in use, so the UI never labels a list with a
/// model that did not produce it.
/// </summary>
public sealed record PriorityListPageView(
    IReadOnlyList<PriorityItemView> Items,
    string? NextCursor,
    string Mode,
    string MlStatus,
    string? ModelProvenance);
