using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Campaigns.Requests;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Campaigns.Features.Templates.Archive;
using Parley.Campaigns.Features.Templates.Create;
using Parley.Campaigns.Features.Templates.Get;
using Parley.Campaigns.Features.Templates.List;
using Parley.Campaigns.Features.Templates.Preview;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Campaigns.Features.Templates.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Campaigns;

/// <summary>Email templates: library list, create, read, replace, archive and sample preview.</summary>
public sealed class TemplatesController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CampaignsRoutes.Templates)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<KeysetCursorPage<TemplateSummaryView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListTemplatesQuery(after, limit, q), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CampaignsRoutes.Templates)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType<TemplateView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] TemplateRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateTemplateCommand(request?.Name ?? string.Empty, request?.SubjectLiquid ?? string.Empty, request?.BodyLiquid ?? string.Empty, request?.Locale ?? string.Empty),
            cancellationToken);
        return FromResult(result, view => Created($"{CampaignsRoutes.Templates}/{view.Id:D}", view));
    }

    [HttpGet(CampaignsRoutes.TemplateById)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<TemplateView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTemplateQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(CampaignsRoutes.TemplateById)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType<TemplateView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TemplateRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateTemplateCommand(id, request?.Name ?? string.Empty, request?.SubjectLiquid ?? string.Empty, request?.BodyLiquid ?? string.Empty, request?.Locale ?? string.Empty, request?.Version ?? default),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CampaignsRoutes.TemplateById)]
    [RequiresPermission(Permissions.CampaignsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ArchiveTemplateCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost(CampaignsRoutes.TemplatePreview)]
    [RequiresPermission(Permissions.CampaignsRead)]
    [ProducesResponseType<TemplatePreviewView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PreviewTemplateQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }
}
