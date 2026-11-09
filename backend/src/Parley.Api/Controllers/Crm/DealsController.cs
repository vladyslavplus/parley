using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Crm.Requests;
using Parley.Api.Localization;
using Parley.Crm.Features.Deals.Create;
using Parley.Crm.Features.Deals.Delete;
using Parley.Crm.Features.Deals.Get;
using Parley.Crm.Features.Deals.List;
using Parley.Crm.Features.Deals.Move;
using Parley.Crm.Features.Deals.Restore;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Features.Deals.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Deals: list, read, create, update, soft delete and move between stages.</summary>
public sealed class DealsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Deals)]
    [RequiresPermission(Permissions.DealsRead)]
    [ProducesResponseType<KeysetCursorPage<DealView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] Guid? stageId,
        [FromQuery] Guid? ownerUserId,
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? contactId,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListDealsQuery(after, limit, q, stageId, ownerUserId, companyId, contactId, status, sort, order),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CrmRoutes.DealById)]
    [RequiresPermission(Permissions.DealsRead)]
    [ProducesResponseType<DealView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDealQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.Deals)]
    [RequiresPermission(Permissions.DealsWrite)]
    [ProducesResponseType<DealView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] DealRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateDealCommand(
                request?.Title ?? string.Empty,
                request?.Amount ?? default,
                request?.Currency ?? string.Empty,
                request?.ContactId,
                request?.CompanyId,
                request?.OwnerUserId,
                request?.ExpectedCloseOn,
                request?.StageId,
                request?.CustomFields),
            cancellationToken);
        return FromResult(result, view => Created($"{CrmRoutes.Deals}/{view.Id:D}", view));
    }

    [HttpPut(CrmRoutes.DealById)]
    [RequiresPermission(Permissions.DealsWrite)]
    [ProducesResponseType<DealView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDealRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateDealCommand(
                id,
                request?.Version ?? default,
                request?.Title ?? string.Empty,
                request?.Amount ?? default,
                request?.Currency ?? string.Empty,
                request?.ContactId,
                request?.CompanyId,
                request?.OwnerUserId,
                request?.ExpectedCloseOn,
                request?.CustomFields),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CrmRoutes.DealById)]
    [RequiresPermission(Permissions.DealsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteDealCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost(CrmRoutes.DealRestore)]
    [RequiresPermission(Permissions.DealsDelete)]
    [ProducesResponseType<DealView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestoreDealCommand(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.DealMove)]
    [RequiresPermission(Permissions.DealsWrite)]
    [ProducesResponseType<DealView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveDealRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new MoveDealCommand(id, request?.StageId ?? Guid.Empty, request?.Position ?? default, request?.Version ?? default, request?.LostReason),
            cancellationToken);
        return FromResult(result, Ok);
    }
}
