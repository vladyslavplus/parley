using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Features.Activities.Shared;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Activities;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Subjects;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Crm.Features.Activities.List;

public sealed class ListActivitiesQueryHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory) : IRequestHandler<ListActivitiesQuery, Result<KeysetCursorPage<ActivityView>>>
{
    private const string OccurredSortField = "occurred";

    public async ValueTask<Result<KeysetCursorPage<ActivityView>>> Handle(ListActivitiesQuery query, CancellationToken cancellationToken)
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
            return Result<KeysetCursorPage<ActivityView>>.Failure(Error.NotFound(CrmErrorCodes.ActivitySubjectNotFound));
        }

        var position = KeysetOrdering.ReadValuePosition(cursor, OccurredSortField, KeysetOrdering.ParseTimestamp);
        if (cursor is not null && position is null)
        {
            return Failure();
        }

        var visibleContacts = await DealLookup.VisibleContactsAsync(dbContext, dataScope, cancellationToken);
        var visibleDeals = await DealLookup.VisibleDealsAsync(dbContext, dataScope, cancellationToken);
        var visibleCompanies = await DealLookup.VisibleCompaniesAsync(dbContext, dataScope, cancellationToken);
        var liveNotes = dbContext.Notes.AsNoTracking().Where(note => note.DeletedAt == null);
        var liveTasks = dbContext.Tasks.AsNoTracking().Where(task => task.DeletedAt == null);

        var entries = BySubject(dbContext.Activities.AsNoTracking(), query)
            .Where(activity => (activity.NoteId == null || liveNotes.Any(note => note.Id == activity.NoteId))
                && (activity.TaskId == null || liveTasks.Any(task => task.Id == activity.TaskId)));
        if (query.Type is not null)
        {
            entries = entries.Where(activity => activity.Type == query.Type);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search);
            entries = entries.Where(activity =>
                (activity.NoteId != null && liveNotes.Any(note => note.Id == activity.NoteId && EF.Functions.ILike(note.Body, pattern)))
                || (activity.TaskId != null && liveTasks.Any(task => task.Id == activity.TaskId && EF.Functions.ILike(task.TitleNormalized, pattern))));
        }

        var rows = await KeysetOrdering.Apply(entries, activity => activity.OccurredAt, activity => activity.Id, position, descending)
            .Select(ActivityViews.RowProjectionFor(visibleContacts, visibleDeals, visibleCompanies, liveNotes, liveTasks))
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        var page = KeysetCursorPage<ActivityRow>.From(rows, limit, row => KeysetOrdering.CursorFor(OccurredSortField, KeysetOrdering.FormatTimestamp(row.OccurredAt), row.Id));
        var actors = await ownerDirectory.ReadAsync(page.Items.Select(row => row.ActorUserId), cancellationToken);
        var items = page.Items.Select(row => ActivityViews.ToView(row, actors)).ToArray();
        return Result<KeysetCursorPage<ActivityView>>.Success(new KeysetCursorPage<ActivityView>(items, page.NextCursor));
    }

    /// <summary>Keeps the entries of the one subject the query names; the validator guarantees exactly one.</summary>
    private static IQueryable<Activity> BySubject(IQueryable<Activity> activities, ListActivitiesQuery query)
    {
        if (query.ContactId is { } contactId)
        {
            return activities.Where(activity => activity.ContactId == contactId);
        }

        if (query.DealId is { } dealId)
        {
            return activities.Where(activity => activity.DealId == dealId);
        }

        return activities.Where(activity => activity.CompanyId == query.CompanyId);
    }

    private static Result<KeysetCursorPage<ActivityView>> Failure() =>
        Result<KeysetCursorPage<ActivityView>>.Failure(CrmErrors.Field(nameof(ListActivitiesQuery.After), ErrorCodes.ValidationFailed));
}
