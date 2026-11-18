using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Priority.Segments;

/// <summary>
/// Segment counts for the latest priority day: the contacts of each RFM and uplift segment that are on the day's list, and
/// the size of each segment in the caller's scope. The list endpoint can never answer how large a segment is, because it
/// returns a capped top list.
/// </summary>
[RequiresPermission(Permissions.InsightsView)]
public sealed record GetPrioritySegmentCountsQuery : IRequest<Result<PrioritySegmentCountsView>>;
