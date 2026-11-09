using Mediator;
using Parley.Crm.Features.Notes.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Notes.List;

/// <summary>
/// Lists the notes of one contact, deal or company, newest first by default, keyset-paged. Exactly one subject is
/// required; a subject the caller may not read returns not found.
/// </summary>
/// <param name="After">Cursor of the previous page, or null for the first page.</param>
/// <param name="Limit">Page size; clamped to the allowed range.</param>
/// <param name="Search">Case-insensitive text the note body must contain.</param>
/// <param name="ContactId">Notes of this contact.</param>
/// <param name="DealId">Notes of this deal.</param>
/// <param name="CompanyId">Notes of this company.</param>
/// <param name="Order">Sort direction by creation instant: <c>desc</c> (default) or <c>asc</c>.</param>
[RequiresPermission(Permissions.ActivitiesRead)]
public sealed record ListNotesQuery(
    string? After,
    int? Limit,
    string? Search,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId,
    string? Order) : IRequest<Result<KeysetCursorPage<NoteView>>>;
