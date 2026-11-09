using Mediator;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Create;

/// <summary>Creates a segment. A filter the module cannot evaluate yet is stored and reported by the preview.</summary>
/// <param name="Name">Segment name.</param>
/// <param name="Definition">The typed filter tree.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record CreateSegmentCommand(string Name, SegmentDefinition Definition) : IRequest<Result<SegmentView>>;
