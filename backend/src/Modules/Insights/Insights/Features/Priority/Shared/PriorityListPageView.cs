namespace Parley.Insights.Features.Priority.Shared;

/// <summary>
/// One keyset page of the priority list, with the mode the list was built in and the ML status that explains it. The
/// <c>items</c> and <c>nextCursor</c> keys are those of a keyset page; <c>mode</c> is <c>ml</c> or <c>rfm</c>, and
/// <c>mlStatus</c> is one of the values of the priority ML status.
/// </summary>
public sealed record PriorityListPageView(
    IReadOnlyList<PriorityItemView> Items,
    string? NextCursor,
    string Mode,
    string MlStatus);
