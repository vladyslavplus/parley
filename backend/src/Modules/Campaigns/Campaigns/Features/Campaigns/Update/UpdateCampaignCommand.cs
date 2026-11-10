using Mediator;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Update;

/// <summary>Replaces the settings of a draft campaign. Answered with 409 once the audience is frozen or the campaign left draft.</summary>
/// <param name="Id">Campaign to change.</param>
/// <param name="Name">Campaign name.</param>
/// <param name="TemplateId">Template to send.</param>
/// <param name="SegmentId">Segment of the audience, or null for every contact with an address.</param>
/// <param name="HoldoutShareBps">Holdout share; null takes the configured default.</param>
/// <param name="TargetingMode">Targeting mode.</param>
/// <param name="TargetingShareBps">Share a ranking targets; null for all.</param>
/// <param name="Version">Row version the client read.</param>
[RequiresPermission(Permissions.CampaignsManage)]
public sealed record UpdateCampaignCommand(
    Guid Id,
    string Name,
    Guid TemplateId,
    Guid? SegmentId,
    int? HoldoutShareBps,
    string TargetingMode,
    int? TargetingShareBps,
    uint Version) : IRequest<Result<CampaignView>>;
