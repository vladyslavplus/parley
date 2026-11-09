using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Notes;
using Parley.Crm.Features.Notes.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Notes;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Subjects;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Crm.Features.Notes.List;

public sealed class ListNotesQueryHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory) : IRequestHandler<ListNotesQuery, Result<KeysetCursorPage<NoteView>>>
{
    private const string CreatedSortField = "created";

    public async ValueTask<Result<KeysetCursorPage<NoteView>>> Handle(ListNotesQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var descending = SortDirections.IsDescending(query.Order, defaultDescending: true);

        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var unreadable = await SubjectChecks.FirstUnreadableAsync(dbContext, dataScope, query.ContactId, query.DealId, query.CompanyId, cancellationToken);
        if (unreadable is not null)
        {
            return Result<KeysetCursorPage<NoteView>>.Failure(Error.NotFound(CrmErrorCodes.NoteSubjectNotFound));
        }

        var position = KeysetOrdering.ReadValuePosition(cursor, CreatedSortField, KeysetOrdering.ParseTimestamp);
        if (cursor is not null && position is null)
        {
            return Failure();
        }

        var notes = await NoteLookup.VisibleNotesAsync(dbContext, dataScope, cancellationToken);
        notes = BySubject(notes, query);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search);
            notes = notes.Where(note => EF.Functions.ILike(note.Body, pattern));
        }

        var rows = await KeysetOrdering.Apply(notes, note => note.CreatedAt, note => note.Id, position, descending)
            .Select(NoteViews.RowProjection())
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        var page = KeysetCursorPage<NoteRow>.From(rows, limit, row => KeysetOrdering.CursorFor(CreatedSortField, KeysetOrdering.FormatTimestamp(row.CreatedAt), row.Id));
        var authors = await ownerDirectory.ReadAsync(page.Items.Select(row => (Guid?)row.AuthorUserId), cancellationToken);
        var items = page.Items.Select(row => NoteViews.ToView(row, authors)).ToArray();
        return Result<KeysetCursorPage<NoteView>>.Success(new KeysetCursorPage<NoteView>(items, page.NextCursor));
    }

    /// <summary>Keeps the notes of the one subject the query names; the validator guarantees exactly one.</summary>
    private static IQueryable<Note> BySubject(IQueryable<Note> notes, ListNotesQuery query)
    {
        if (query.ContactId is { } contactId)
        {
            return notes.Where(note => note.ContactId == contactId);
        }

        if (query.DealId is { } dealId)
        {
            return notes.Where(note => note.DealId == dealId);
        }

        return notes.Where(note => note.CompanyId == query.CompanyId);
    }

    private static Result<KeysetCursorPage<NoteView>> Failure() =>
        Result<KeysetCursorPage<NoteView>>.Failure(CrmErrors.Field(nameof(ListNotesQuery.After), ErrorCodes.ValidationFailed));
}
