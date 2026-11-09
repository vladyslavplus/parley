using Mediator;
using Parley.Crm.Features.Companies.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.List;

/// <summary>
/// Lists the companies the caller may see, keyset-paged. Ties on the sort value are broken by identifier.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Search">Case-insensitive text matched against the name and the domain.</param>
/// <param name="OwnerUserId">Only companies owned by this user.</param>
/// <param name="Sort">Sort field: <c>name</c> (default) or <c>created</c>.</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.CompaniesRead)]
public sealed record ListCompaniesQuery(
    string? After,
    int? Limit,
    string? Search,
    Guid? OwnerUserId,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<CompanyView>>>;
