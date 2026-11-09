using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Campaigns.Requests;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Segments.Archive;
using Parley.Campaigns.Features.Segments.Create;
using Parley.Campaigns.Features.Segments.Get;
using Parley.Campaigns.Features.Segments.List;
using Parley.Campaigns.Features.Segments.Preview;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Features.Segments.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Campaigns;

/// <summary>Audience segments: library list, create, read, replace, archive and the count preview.</summary>
public sealed class SegmentsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CampaignsRoutes.Segments)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<KeysetCursorPage<SegmentSummaryView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListSegmentsQuery(after, limit, q), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CampaignsRoutes.Segments)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType<SegmentView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] SegmentRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateSegmentCommand(request?.Name ?? string.Empty, request?.Definition?.ToDefinition() ?? new SegmentDefinition()),
            cancellationToken);
        return FromResult(result, view => Created($"{CampaignsRoutes.Segments}/{view.Id:D}", view));
    }

    [HttpGet(CampaignsRoutes.SegmentById)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<SegmentView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSegmentQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(CampaignsRoutes.SegmentById)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType<SegmentView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SegmentRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateSegmentCommand(id, request?.Name ?? string.Empty, request?.Definition?.ToDefinition() ?? new SegmentDefinition(), request?.Version ?? default),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CampaignsRoutes.SegmentById)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ArchiveSegmentCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost(CampaignsRoutes.SegmentPreview)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType<SegmentPreviewView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Preview([FromBody] SegmentDefinitionRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PreviewSegmentQuery(request?.ToDefinition() ?? new SegmentDefinition()), cancellationToken);
        return FromResult(result, Ok);
    }
}
