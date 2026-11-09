using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Campaigns.Requests;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Features.Campaigns.Create;
using Parley.Campaigns.Features.Campaigns.Prepare;
using Parley.Campaigns.Features.Campaigns.Review;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Campaigns.Features.Campaigns.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Campaigns;

/// <summary>Campaigns: create and edit a draft, prepare the audience, and read the experiment review.</summary>
public sealed class CampaignsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpPost(CampaignsRoutes.Campaigns)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType<CampaignView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CampaignRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateCampaignCommand(
                request?.Name ?? string.Empty,
                request?.TemplateId ?? Guid.Empty,
                request?.SegmentId,
                request?.HoldoutShareBps,
                request?.TargetingMode ?? CampaignTargetingModes.All,
                request?.TargetingShareBps),
            cancellationToken);
        return FromResult(result, view => Created($"{CampaignsRoutes.Campaigns}/{view.Id:D}", view));
    }

    [HttpPut(CampaignsRoutes.CampaignById)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType<CampaignView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CampaignRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateCampaignCommand(
                id,
                request?.Name ?? string.Empty,
                request?.TemplateId ?? Guid.Empty,
                request?.SegmentId,
                request?.HoldoutShareBps,
                request?.TargetingMode ?? CampaignTargetingModes.All,
                request?.TargetingShareBps,
                request?.Version ?? default),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CampaignsRoutes.CampaignPrepare)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType<CampaignView>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Prepare(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PrepareCampaignCommand(id), cancellationToken);
        return FromResult(result, Accepted);
    }

    [HttpGet(CampaignsRoutes.CampaignReview)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<CampaignReviewView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Review(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReviewCampaignQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }
}
