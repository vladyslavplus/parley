using Mediator;
using Parley.Crm.Features.Deals.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Deals.List;

/// <summary>
/// Lists the deals the caller may see, keyset-paged, with the filters of the board and of the deal list. Ties on the
/// sort value are broken by identifier, so every row appears exactly once across pages.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Search">Case-insensitive text the title must contain.</param>
/// <param name="StageId">Only deals on this stage.</param>
/// <param name="OwnerUserId">Only deals owned by this user.</param>
/// <param name="CompanyId">Only deals of this company.</param>
/// <param name="ContactId">Only deals of this contact.</param>
/// <param name="Status">Only deals with this status: <c>open</c>, <c>won</c> or <c>lost</c>.</param>
/// <param name="Sort">Sort field: <c>name</c> (default), <c>amount</c>, <c>created</c> or <c>position</c>.</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.DealsRead)]
public sealed record ListDealsQuery(
    string? After,
    int? Limit,
    string? Search,
    Guid? StageId,
    Guid? OwnerUserId,
    Guid? CompanyId,
    Guid? ContactId,
    string? Status,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<DealView>>>;
