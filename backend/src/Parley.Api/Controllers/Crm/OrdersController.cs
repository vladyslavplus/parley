using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Crm.Features.Orders.Get;
using Parley.Crm.Features.Orders.List;
using Parley.Crm.Features.Orders.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Read-only order history of the contacts.</summary>
public sealed class OrdersController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    /// <summary>Lists the orders the caller may see, newest first by default.</summary>
    /// <param name="after">Opaque cursor of the page after the one before.</param>
    /// <param name="limit">Page size.</param>
    /// <param name="q">Free-text search over the order's invoice number and the contact's name.</param>
    /// <param name="contactId">Only orders of this contact.</param>
    /// <param name="companyId">Only orders of contacts of this company.</param>
    /// <param name="orderedFrom">Earliest order instant, inclusive, ISO 8601 with an offset. Clients convert a calendar day in their own time zone to an instant.</param>
    /// <param name="orderedTo">Order instant the range ends before, exclusive, ISO 8601 with an offset. Must not be earlier than <c>orderedFrom</c>.</param>
    /// <param name="sort">Sort field.</param>
    /// <param name="order">Sort direction, <c>asc</c> or <c>desc</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    [HttpGet(CrmRoutes.Orders)]
    [RequiresPermission(Permissions.OrdersRead)]
    [ProducesResponseType<KeysetCursorPage<OrderView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] Guid? contactId,
        [FromQuery] Guid? companyId,
        [FromQuery] DateTimeOffset? orderedFrom,
        [FromQuery] DateTimeOffset? orderedTo,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListOrdersQuery(after, limit, q, contactId, companyId, orderedFrom, orderedTo, sort, order),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CrmRoutes.OrderById)]
    [RequiresPermission(Permissions.OrdersRead)]
    [ProducesResponseType<OrderView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOrderQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }
}
