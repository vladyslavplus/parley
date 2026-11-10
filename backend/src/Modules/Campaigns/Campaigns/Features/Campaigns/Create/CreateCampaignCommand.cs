using Mediator;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Create;

/// <summary>Creates a draft campaign. Its audience is frozen later, by the prepare command.</summary>
/// <param name="Name">Campaign name.</param>
/// <param name="TemplateId">Template to send.</param>
/// <param name="SegmentId">Segment of the audience, or null for every contact with an address.</param>
/// <param name="HoldoutShareBps">Holdout share; null takes the configured default.</param>
/// <param name="TargetingMode">Targeting mode; ranking by score needs the insights permission.</param>
/// <param name="TargetingShareBps">Share a ranking targets; required for ranking modes, null for all.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record CreateCampaignCommand(
    string Name,
    Guid TemplateId,
    Guid? SegmentId,
    int? HoldoutShareBps,
    string TargetingMode,
    int? TargetingShareBps) : IRequest<Result<CampaignView>>;
