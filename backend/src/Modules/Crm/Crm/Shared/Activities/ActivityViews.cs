using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Notes;
using Parley.Crm.Domain.Tasks;
using Parley.Crm.Features.Activities.Shared;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Users;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Shared.Activities;

/// <summary>
/// Projection and mapping of timeline entries. The note text and the task title are read only while the note or task
/// is live. The actor name comes from the user directory, one call per page.
/// </summary>
public static class ActivityViews
{
    /// <summary>
    /// Row projection. Names of records outside the caller's scope stay empty; a note or task that was deleted shows no text.
    /// </summary>
    public static Expression<Func<Activity, ActivityRow>> RowProjectionFor(
        IQueryable<Contact> visibleContacts,
        IQueryable<Deal> visibleDeals,
        IQueryable<Company> visibleCompanies,
        IQueryable<Note> liveNotes,
        IQueryable<CrmTask> liveTasks) => activity => new ActivityRow(
        activity.Id,
        activity.Type,
        activity.OccurredAt,
        activity.ActorUserId,
        activity.ContactId,
        visibleContacts.Where(contact => contact.Id == activity.ContactId).Select(contact => contact.FirstName).FirstOrDefault(),
        visibleContacts.Where(contact => contact.Id == activity.ContactId).Select(contact => contact.LastName).FirstOrDefault(),
        activity.DealId,
        visibleDeals.Where(deal => deal.Id == activity.DealId).Select(deal => deal.Title).FirstOrDefault(),
        activity.CompanyId,
        visibleCompanies.Where(company => company.Id == activity.CompanyId).Select(company => company.Name).FirstOrDefault(),
        activity.NoteId,
        liveNotes.Where(note => note.Id == activity.NoteId).Select(note => note.Body).FirstOrDefault(),
        liveNotes.Where(note => note.Id == activity.NoteId).Select(note => (uint?)EF.Property<uint>(note, PersistenceConstants.XminPropertyName)).FirstOrDefault(),
        activity.TaskId,
        liveTasks.Where(task => task.Id == activity.TaskId).Select(task => task.Title).FirstOrDefault(),
        activity.Payload);

    public static ActivityView ToView(ActivityRow row, IReadOnlyDictionary<Guid, UserDirectoryEntry> actors)
    {
        var actor = OwnerDirectory.Find(actors, row.ActorUserId);
        return new ActivityView(
            row.Id,
            row.Type,
            row.OccurredAt,
            row.ActorUserId,
            actor?.DisplayName,
            row.ContactId,
            PersonNames.Join(row.ContactFirstName, row.ContactLastName),
            row.DealId,
            row.DealTitle,
            row.CompanyId,
            row.CompanyName,
            row.NoteId,
            row.NoteBody,
            row.NoteVersion,
            row.TaskId,
            row.TaskTitle,
            JsonDocument.Parse(row.Payload).RootElement.Clone());
    }
}
