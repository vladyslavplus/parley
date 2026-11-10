using Mediator;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Update;

/// <summary>
/// Replaces the name and filter definition of a segment. The version is the row version the client read; a segment
/// changed since then answers 409.
/// </summary>
/// <param name="Id">Segment identifier.</param>
/// <param name="Name">Segment name.</param>
/// <param name="Definition">The typed filter tree.</param>
/// <param name="Version">Row version the client read.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record UpdateSegmentCommand(Guid Id, string Name, SegmentDefinition Definition, uint Version)
    : IRequest<Result<SegmentView>>;
