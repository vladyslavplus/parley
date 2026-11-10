using Mediator;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Get;

/// <summary>Reads one segment with its definition.</summary>
/// <param name="Id">Segment identifier.</param>
[RequiresPermission(Permissions.CampaignsRead)]
public sealed record GetSegmentQuery(Guid Id) : IRequest<Result<SegmentView>>;
