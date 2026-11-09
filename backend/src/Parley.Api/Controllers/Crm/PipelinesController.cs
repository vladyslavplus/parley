using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Crm.Requests;
using Parley.Api.Localization;
using Parley.Crm.Features.Pipelines.Board;
using Parley.Crm.Features.Pipelines.Get;
using Parley.Crm.Features.Pipelines.List;
using Parley.Crm.Features.Pipelines.Rename;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Crm.Features.Pipelines.Stages.Create;
using Parley.Crm.Features.Pipelines.Stages.Delete;
using Parley.Crm.Features.Pipelines.Stages.Reorder;
using Parley.Crm.Features.Pipelines.Stages.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Pipelines: list and read, the board, rename, and the stage settings (add, change, remove, reorder).</summary>
public sealed class PipelinesController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Pipelines)]
    [RequiresPermission(Permissions.DealsRead)]
    [ProducesResponseType<KeysetCursorPage<PipelineSummaryView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListPipelinesQuery(after, limit, q, sort, order), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CrmRoutes.PipelineById)]
    [RequiresPermission(Permissions.DealsRead)]
    [ProducesResponseType<PipelineView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPipelineQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(CrmRoutes.PipelineById)]
    [RequiresPermission(Permissions.PipelinesManage)]
    [ProducesResponseType<PipelineView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Rename(Guid id, [FromBody] PipelineRenameRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RenamePipelineCommand(id, request?.Version ?? default, request?.Name ?? string.Empty), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CrmRoutes.PipelineBoard)]
    [RequiresPermission(Permissions.DealsRead)]
    [ProducesResponseType<PipelineBoardView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Board(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPipelineBoardQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.PipelineStages)]
    [RequiresPermission(Permissions.PipelinesManage)]
    [ProducesResponseType<PipelineView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStage(Guid id, [FromBody] PipelineStageRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreatePipelineStageCommand(id, request?.Name ?? string.Empty, request?.ColorIndex ?? default, request?.ProbabilityBps ?? default),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(CrmRoutes.PipelineStageById)]
    [RequiresPermission(Permissions.PipelinesManage)]
    [ProducesResponseType<PipelineView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStage(Guid id, Guid stageId, [FromBody] PipelineStageRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdatePipelineStageCommand(id, stageId, request?.Name ?? string.Empty, request?.ColorIndex ?? default, request?.ProbabilityBps ?? default),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CrmRoutes.PipelineStageById)]
    [RequiresPermission(Permissions.PipelinesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteStage(Guid id, Guid stageId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeletePipelineStageCommand(id, stageId), cancellationToken);
        return FromResult(result);
    }

    [HttpPut(CrmRoutes.PipelineStageOrder)]
    [RequiresPermission(Permissions.PipelinesManage)]
    [ProducesResponseType<PipelineView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderStages(Guid id, [FromBody] PipelineStageOrderRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReorderPipelineStagesCommand(id, request?.StageIds ?? []), cancellationToken);
        return FromResult(result, Ok);
    }
}
