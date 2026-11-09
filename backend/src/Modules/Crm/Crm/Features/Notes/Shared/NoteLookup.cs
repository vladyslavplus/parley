using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Notes;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Notes;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Features.Notes.Shared;

/// <summary>
/// Scoped note lookups. A note is visible when it is live and the caller may read the contact, deal or company it
/// belongs to; a note outside that set is reported as not found.
/// </summary>
internal static class NoteLookup
{
    /// <summary>Live notes the caller may read.</summary>
    public static async Task<IQueryable<Note>> VisibleNotesAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        CancellationToken cancellationToken)
    {
        var contacts = await DealLookup.VisibleContactsAsync(dbContext, dataScope, cancellationToken);
        var deals = await DealLookup.VisibleDealsAsync(dbContext, dataScope, cancellationToken);
        var companies = await DealLookup.VisibleCompaniesAsync(dbContext, dataScope, cancellationToken);
        return dbContext.Notes.AsNoTracking().Where(note =>
            note.DeletedAt == null
            && ((note.ContactId != null && contacts.Any(contact => contact.Id == note.ContactId))
                || (note.DealId != null && deals.Any(deal => deal.Id == note.DealId))
                || (note.CompanyId != null && companies.Any(company => company.Id == note.CompanyId))));
    }

    /// <summary>A tracked note the caller may change, or null when missing, deleted or out of scope.</summary>
    public static async Task<Note?> FindTrackedAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        Guid id,
        CancellationToken cancellationToken)
    {
        // The visibility check runs as its own query: a no-tracking subquery inside the tracked read would leave the note untracked.
        var note = await dbContext.Notes.FirstOrDefaultAsync(row => row.Id == id && row.DeletedAt == null, cancellationToken);
        if (note is null)
        {
            return null;
        }

        var visible = await VisibleNotesAsync(dbContext, dataScope, cancellationToken);
        return await visible.AnyAsync(row => row.Id == id, cancellationToken) ? note : null;
    }

    /// <summary>
    /// A tracked note the caller may change: its author, or any caller whose <c>activities.write</c> scope is
    /// organization-wide. Other callers get null, reported as not found.
    /// </summary>
    public static async Task<Note?> FindChangeableAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        Guid actorId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var note = await FindTrackedAsync(dbContext, dataScope, id, cancellationToken);
        if (note is null || note.AuthorUserId == actorId)
        {
            return note;
        }

        var scope = await dataScope.ForPermissionAsync(Permissions.ActivitiesWrite, cancellationToken);
        return scope.Level == DataScopeLevel.Organization ? note : null;
    }

    /// <summary>A note row the caller may read, or null when missing, deleted or out of scope.</summary>
    public static async Task<NoteRow?> FindRowAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        Guid id,
        CancellationToken cancellationToken)
    {
        var visible = await VisibleNotesAsync(dbContext, dataScope, cancellationToken);
        return await visible.Where(note => note.Id == id).Select(NoteViews.RowProjection()).FirstOrDefaultAsync(cancellationToken);
    }
}
