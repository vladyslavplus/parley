using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Campaigns.Requests;
using Parley.Api.Controllers.Common;
using Parley.Api.Idempotency;
using Parley.Api.Localization;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Features.Campaigns.Create;
using Parley.Campaigns.Features.Campaigns.Get;
using Parley.Campaigns.Features.Campaigns.List;
using Parley.Campaigns.Features.Campaigns.Prepare;
using Parley.Campaigns.Features.Campaigns.Progress;
using Parley.Campaigns.Features.Campaigns.Recipients;
using Parley.Campaigns.Features.Campaigns.Results;
using Parley.Campaigns.Features.Campaigns.Review;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Campaigns.Features.Campaigns.Start;
using Parley.Campaigns.Features.Campaigns.TestSend;
using Parley.Campaigns.Features.Campaigns.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Campaigns;

/// <summary>Campaigns: create and edit a draft, prepare the audience, and read the experiment review.</summary>
public sealed class CampaignsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CampaignsRoutes.Campaigns)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<KeysetCursorPage<CampaignSummaryView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListCampaignsQuery(after, limit, q, status, sort, order), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CampaignsRoutes.CampaignById)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<CampaignView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCampaignQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

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

    [HttpPost(CampaignsRoutes.CampaignStart)]
    [CampaignsIdempotencyKey]
    [RequiresPermission(Permissions.CampaignsSend)]
    [ProducesResponseType<CampaignView>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new StartCampaignCommand(id), cancellationToken);
        return FromResult(result, Accepted);
    }

    [HttpGet(CampaignsRoutes.CampaignProgress)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<CampaignProgressView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Progress(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCampaignProgressQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CampaignsRoutes.CampaignResults)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<CampaignResultsView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Results(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCampaignResultsQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    /// <summary>Queues a fresh attribution of a completed campaign. The result is written by the job; poll the results endpoint.</summary>
    [HttpPost(CampaignsRoutes.CampaignResultsRefresh)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RefreshResults(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RefreshCampaignResultsCommand(id), cancellationToken);
        return result.IsSuccess ? Accepted() : FromResult(result);
    }

    /// <summary>
    /// One keyset page of the recipients of a campaign, in freeze order. Filters: arm, targeted, send status, converted,
    /// and a search by contact name. The address is shown only to callers who see the contact's address at organization
    /// scope; the rest get it as null. Empty until the audience is frozen.
    /// </summary>
    [HttpGet(CampaignsRoutes.CampaignRecipients)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<KeysetCursorPage<CampaignRecipientView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Recipients(
        Guid id,
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] string? arm,
        [FromQuery] bool? targeted,
        [FromQuery] string? sendStatus,
        [FromQuery] bool? converted,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListCampaignRecipientsQuery(id, after, limit, q, arm, targeted, sendStatus, converted, sort, order),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CampaignsRoutes.CampaignTestSend)]
    [RequiresPermission(Permissions.CampaignsSend)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestSend(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SendTestEmailCommand(id), cancellationToken);
        return FromResult(result);
    }
}
