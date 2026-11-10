using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Features.Priority.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Priority.List;

/// <summary>Lists one keyset page of the latest priority list, highest value first, limited to the caller's data scope.</summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Segment">Only contacts in this RFM segment; null for every segment.</param>
[RequiresPermission(Permissions.InsightsView)]
public sealed record ListPriorityQuery(string? After, int? Limit, string? Segment)
    : IRequest<Result<KeysetCursorPage<PriorityItemView>>>;
