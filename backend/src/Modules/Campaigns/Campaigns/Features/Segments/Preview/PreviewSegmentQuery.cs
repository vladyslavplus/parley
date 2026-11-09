using Mediator;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Preview;

/// <summary>
/// Counts the eligible contacts a definition selects and returns a sample of their identifiers. Needs the
/// marketing permission because the count covers the whole organization, as the campaign audience does.
/// </summary>
/// <param name="Definition">The typed filter tree to count.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record PreviewSegmentQuery(SegmentDefinition Definition) : IRequest<Result<SegmentPreviewView>>;
