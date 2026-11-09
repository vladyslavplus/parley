using Mediator;
using Parley.Crm.Features.Orders.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Orders.List;

/// <summary>
/// Lists the orders the caller may see, keyset-paged. An order is visible when its contact is visible under the caller's
/// <c>orders.read</c> data scope. Ties on the sort value are broken by identifier.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Search">Case-insensitive text the invoice number must contain.</param>
/// <param name="ContactId">Only orders of this contact.</param>
/// <param name="CompanyId">Only orders of contacts of this company.</param>
/// <param name="OrderedFrom">Earliest order instant, inclusive, UTC offset allowed. Clients compute day bounds in their own time zone.</param>
/// <param name="OrderedTo">Order instant the range ends before, exclusive, UTC offset allowed. Must not be earlier than <paramref name="OrderedFrom"/>.</param>
/// <param name="Sort">Sort field: <c>ordered</c> (default) or <c>amount</c>.</param>
/// <param name="Order">Sort direction: <c>desc</c> (default) or <c>asc</c>.</param>
[RequiresPermission(Permissions.OrdersRead)]
public sealed record ListOrdersQuery(
    string? After,
    int? Limit,
    string? Search,
    Guid? ContactId,
    Guid? CompanyId,
    DateTimeOffset? OrderedFrom,
    DateTimeOffset? OrderedTo,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<OrderView>>>;
