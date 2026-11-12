using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Rfm.Segments;

/// <summary>Counts the contacts of each RFM segment on the latest snapshot day, limited to the caller's data scope.</summary>
[RequiresPermission(Permissions.InsightsView)]
public sealed record ListRfmSegmentsQuery : IRequest<Result<IReadOnlyList<RfmSegmentCountView>>>;
