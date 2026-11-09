using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Crm.Requests;
using Parley.Api.Localization;
using Parley.Crm.Features.Tasks.Complete;
using Parley.Crm.Features.Tasks.Create;
using Parley.Crm.Features.Tasks.Delete;
using Parley.Crm.Features.Tasks.Get;
using Parley.Crm.Features.Tasks.List;
using Parley.Crm.Features.Tasks.Reopen;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Features.Tasks.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Tasks: list, the caller's own list, read, create, update, complete, reopen and soft delete.</summary>
public sealed class TasksController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Tasks)]
    [RequiresPermission(Permissions.TasksRead)]
    [ProducesResponseType<KeysetCursorPage<TaskView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] Guid? assigneeUserId,
        [FromQuery] Guid? contactId,
        [FromQuery] Guid? dealId,
        [FromQuery] Guid? companyId,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListTasksQuery(after, limit, q, assigneeUserId, Mine: false, contactId, dealId, companyId, status, sort, order),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CrmRoutes.TasksMine)]
    [RequiresPermission(Permissions.TasksRead)]
    [ProducesResponseType<KeysetCursorPage<TaskView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListMine(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] Guid? contactId,
        [FromQuery] Guid? dealId,
        [FromQuery] Guid? companyId,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListTasksQuery(after, limit, q, AssigneeUserId: null, Mine: true, contactId, dealId, companyId, status, sort, order),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CrmRoutes.TaskById)]
    [RequiresPermission(Permissions.TasksRead)]
    [ProducesResponseType<TaskView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTaskQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.Tasks)]
    [RequiresPermission(Permissions.TasksWrite)]
    [ProducesResponseType<TaskView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] TaskRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateTaskCommand(
                request?.Title ?? string.Empty,
                request?.Notes,
                request?.DueAt,
                request?.AssigneeUserId,
                request?.ContactId,
                request?.DealId,
                request?.CompanyId),
            cancellationToken);
        return FromResult(result, view => Created($"{CrmRoutes.Tasks}/{view.Id:D}", view));
    }

    [HttpPut(CrmRoutes.TaskById)]
    [RequiresPermission(Permissions.TasksWrite)]
    [ProducesResponseType<TaskView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateTaskCommand(
                id,
                request?.Version ?? default,
                request?.Title ?? string.Empty,
                request?.Notes,
                request?.DueAt,
                request?.AssigneeUserId,
                request?.ContactId,
                request?.DealId,
                request?.CompanyId),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.TaskComplete)]
    [RequiresPermission(Permissions.TasksWrite)]
    [ProducesResponseType<TaskView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(Guid id, [FromBody] VersionRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CompleteTaskCommand(id, request?.Version ?? default), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.TaskReopen)]
    [RequiresPermission(Permissions.TasksWrite)]
    [ProducesResponseType<TaskView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reopen(Guid id, [FromBody] VersionRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReopenTaskCommand(id, request?.Version ?? default), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CrmRoutes.TaskById)]
    [RequiresPermission(Permissions.TasksDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTaskCommand(id), cancellationToken);
        return FromResult(result);
    }
}
