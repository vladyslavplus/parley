using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Shared.CustomFields;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Crm.Features.Contacts.Shared;

/// <summary>Projection and mapping of contacts. One SQL statement reads a contact with its tags.</summary>
public static class ContactViews
{
    public static Expression<Func<Contact, ContactRow>> RowProjection => contact => new ContactRow(
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
        contact.NameNormalized);

    /// <summary>Maps a saved contact. The tag identifiers are passed in because the entity does not load them.</summary>
    public static ContactView FromEntity(Contact contact, uint version, IReadOnlyList<Guid> tagIds) => new(
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
        tagIds,
        CustomFieldValues.ToMap(contact.CustomFields),
        contact.CreatedAt,
        contact.UpdatedAt,
        version);

    public static ContactView ToView(ContactRow row) => new(
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
        row.Version);
}
