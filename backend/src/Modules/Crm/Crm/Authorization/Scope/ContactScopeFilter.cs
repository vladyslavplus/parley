using Parley.Crm.Domain.Contacts;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Authorization.Scope;

/// <summary>
/// Restricts contact queries to the live records the current user may see. Unowned contacts are
/// visible at organization level only; a record outside the scope is reported as not found.
/// Soft-deleted contacts are excluded unless the includeDeleted flag is set (restore).
/// </summary>
public static class ContactScopeFilter
{
    public static IQueryable<Contact> Apply(IQueryable<Contact> contacts, DataScope scope, bool includeDeleted = false)
    {
        var rows = includeDeleted ? contacts : contacts.Where(contact => contact.DeletedAt == null);
        return scope.Level switch
        {
            DataScopeLevel.Organization => rows,
            DataScopeLevel.Own or DataScopeLevel.Team => ApplyOwners(rows, scope.OwnerUserIds),
            _ => rows.Where(_ => false),
        };
    }

    private static IQueryable<Contact> ApplyOwners(IQueryable<Contact> contacts, IReadOnlyCollection<Guid>? ownerUserIds)
    {
        var owners = ownerUserIds ?? [];
        return contacts.Where(contact => contact.OwnerUserId != null && owners.Contains(contact.OwnerUserId.Value));
    }
}
