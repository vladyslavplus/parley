using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Shared.Search;

namespace Parley.Crm.Features.Contacts.Shared;

/// <summary>Applies <see cref="ContactListFilter"/> to a contact query. Used by the list and by the export job.</summary>
public static class ContactListFilters
{
    public static IQueryable<Contact> Apply(IQueryable<Contact> contacts, ContactListFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = LikePattern.Contains(filter.Search);
            contacts = contacts.Where(contact => EF.Functions.ILike(contact.NameNormalized, pattern)
                || (contact.EmailNormalized != null && EF.Functions.ILike(contact.EmailNormalized, pattern)));
        }

        if (filter.TagId is { } tagId)
        {
            contacts = contacts.Where(contact => contact.TagLinks.Any(link => link.TagId == tagId));
        }

        if (filter.CompanyId is { } companyId)
        {
            contacts = contacts.Where(contact => contact.CompanyId == companyId);
        }

        if (filter.OwnerUserId is { } ownerUserId)
        {
            contacts = contacts.Where(contact => contact.OwnerUserId == ownerUserId);
        }

        if (filter.CreatedFrom is { } createdFrom)
        {
            var from = DateTime.SpecifyKind(createdFrom.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            contacts = contacts.Where(contact => contact.CreatedAt >= from);
        }

        if (filter.CreatedTo is { } createdTo)
        {
            var toExclusive = DateTime.SpecifyKind(createdTo.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            contacts = contacts.Where(contact => contact.CreatedAt < toExclusive);
        }

        return contacts;
    }
}
