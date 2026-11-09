using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Archive;

/// <summary>Archives a segment: it leaves the library, while past campaigns keep their reference.</summary>
/// <param name="Id">Segment identifier.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record ArchiveSegmentCommand(Guid Id) : IRequest<Result>;
