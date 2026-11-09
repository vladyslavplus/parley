using Mediator;
using Parley.Crm.Features.Activities.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Activities.List;

/// <summary>
/// Reads the timeline of exactly one contact, deal or company, newest first by default, keyset-paged on the instant
/// the entry happened. A subject the caller may not read returns not found.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Search">Case-insensitive text the note text or the task title must contain.</param>
/// <param name="ContactId">Timeline of this contact.</param>
/// <param name="DealId">Timeline of this deal.</param>
/// <param name="CompanyId">Timeline of this company.</param>
/// <param name="Type">Only entries of this type.</param>
/// <param name="Order">Sort direction by instant: <c>desc</c> (default) or <c>asc</c>.</param>
[RequiresPermission(Permissions.ActivitiesRead)]
public sealed record ListActivitiesQuery(
    string? After,
    int? Limit,
    string? Search,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId,
    string? Type,
    string? Order) : IRequest<Result<KeysetCursorPage<ActivityView>>>;
