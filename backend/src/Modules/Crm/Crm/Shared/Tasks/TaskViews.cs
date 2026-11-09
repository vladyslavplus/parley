using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Tasks;
using Parley.Crm.Features.Tasks.Shared;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Users;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Shared.Tasks;

/// <summary>
/// Projection and mapping of tasks. One statement reads a task with the names of its contact, deal and company,
/// limited to the records the caller may see. The assignee name comes from the user directory, one call per page.
/// </summary>
public static class TaskViews
{
    /// <summary>Row projection; a link to a record outside the caller's scope shows no name.</summary>
    public static Expression<Func<CrmTask, TaskRow>> RowProjectionFor(
        IQueryable<Contact> visibleContacts,
        IQueryable<Company> visibleCompanies,
        IQueryable<Deal> visibleDeals) => task => new TaskRow(
        task.Id,
        task.Title,
        task.TitleNormalized,
        task.Notes,
        task.DueAt,
        task.CompletedAt,
        task.AssigneeUserId,
        task.ContactId,
        visibleContacts.Where(contact => contact.Id == task.ContactId).Select(contact => contact.FirstName).FirstOrDefault(),
        visibleContacts.Where(contact => contact.Id == task.ContactId).Select(contact => contact.LastName).FirstOrDefault(),
        task.DealId,
        visibleDeals.Where(deal => deal.Id == task.DealId).Select(deal => deal.Title).FirstOrDefault(),
        task.CompanyId,
        visibleCompanies.Where(company => company.Id == task.CompanyId).Select(company => company.Name).FirstOrDefault(),
        task.CreatedAt,
        task.UpdatedAt,
        EF.Property<uint>(task, PersistenceConstants.XminPropertyName));

    /// <summary>Maps one row, resolving its assignee with a single directory call.</summary>
    public static async Task<TaskView> ToViewAsync(TaskRow row, OwnerDirectory owners, CancellationToken cancellationToken) =>
        ToView(row, await owners.ReadAsync([(Guid?)row.AssigneeUserId], cancellationToken));

    public static TaskView ToView(TaskRow row, IReadOnlyDictionary<Guid, UserDirectoryEntry> owners)
    {
        var assignee = OwnerDirectory.Find(owners, row.AssigneeUserId);
        return new TaskView(
            row.Id,
            row.Title,
            row.Notes,
            row.DueAt,
            row.CompletedAt,
            row.CompletedAt is not null,
            row.AssigneeUserId,
            assignee?.DisplayName,
            assignee?.Email,
            row.ContactId,
            PersonNames.Join(row.ContactFirstName, row.ContactLastName),
            row.DealId,
            row.DealTitle,
            row.CompanyId,
            row.CompanyName,
            row.CreatedAt,
            row.UpdatedAt,
            row.Version);
    }
}
