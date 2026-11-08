using Mediator;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.List;

/// <summary>
/// Lists the contacts the caller may see, keyset-paged. Ties on the sort value are broken by
/// identifier, so every row appears exactly once across pages.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Search">Case-insensitive text matched against the name and the email.</param>
/// <param name="TagId">Only contacts with this tag.</param>
/// <param name="CompanyId">Only contacts of this company.</param>
/// <param name="OwnerUserId">Only contacts owned by this user.</param>
/// <param name="CreatedFrom">First creation day, inclusive, UTC.</param>
/// <param name="CreatedTo">Last creation day, inclusive, UTC.</param>
/// <param name="Sort">Sort field: <c>name</c> (default) or <c>created</c>.</param>
/// <param name="Order">Sort direction: <c>asc</c> (default) or <c>desc</c>.</param>
[RequiresPermission(Permissions.ContactsRead)]
public sealed record ListContactsQuery(
    string? After,
    int? Limit,
    string? Search,
    Guid? TagId,
    Guid? CompanyId,
    Guid? OwnerUserId,
    DateOnly? CreatedFrom,
    DateOnly? CreatedTo,
    string? Sort,
    string? Order) : IRequest<Result<KeysetCursorPage<ContactView>>>;
