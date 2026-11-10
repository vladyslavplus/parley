using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Users;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Features.Contacts.Shared;

/// <summary>
/// Projection and mapping of contacts. One SQL statement reads a contact with its tags and its company
/// name. The owner name is not in that statement: it comes from the user directory in one call per page.
/// </summary>
public static class ContactViews
{
    /// <summary>
    /// Row projection. <paramref name="visibleCompanies"/> limits the company name to companies the caller
    /// may read, so a contact that points at someone else's company does not reveal its name.
    /// </summary>
    public static Expression<Func<Contact, ContactRow>> RowProjectionFor(IQueryable<Company> visibleCompanies) => contact => new ContactRow(
        contact.Id,
        contact.FirstName,
        contact.LastName,
        contact.Email,
        contact.Phone,
        contact.JobTitle,
        contact.Country,
        contact.City,
        contact.CompanyId,
        contact.OwnerUserId,
        contact.CustomFields,
        contact.CreatedAt,
        contact.UpdatedAt,
        EF.Property<uint>(contact, PersistenceConstants.XminPropertyName),
        contact.TagLinks.Select(link => link.TagId).ToList(),
        contact.NameNormalized,
        visibleCompanies.Where(company => company.Id == contact.CompanyId).Select(company => company.Name).FirstOrDefault());

    /// <summary>Maps one row, resolving its owner with a single directory call.</summary>
    public static async Task<ContactView> ToViewAsync(ContactRow row, OwnerDirectory owners, CancellationToken cancellationToken) =>
        ToView(row, await owners.ReadAsync([row.OwnerUserId], cancellationToken));

    public static ContactView ToView(ContactRow row, IReadOnlyDictionary<Guid, UserDirectoryEntry> owners, string? segment = null)
    {
        var owner = OwnerDirectory.Find(owners, row.OwnerUserId);
        return new ContactView(
            row.Id,
            row.FirstName,
            row.LastName,
            row.Email,
            row.Phone,
            row.JobTitle,
            row.Country,
            row.City,
            row.CompanyId,
            row.OwnerUserId,
            row.TagIds,
            CustomFieldValues.ToMap(row.CustomFields),
            row.CreatedAt,
            row.UpdatedAt,
            row.Version,
            owner?.DisplayName,
            owner?.Email,
            row.CompanyName,
            segment);
    }
}
