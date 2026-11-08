using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Features.Contacts.Shared;

/// <summary>Scoped contact lookups shared by the contact handlers.</summary>
internal static class ContactLookup
{
    /// <summary>A tracked contact the caller may change, or null when missing, deleted or out of scope.</summary>
    public static Task<Contact?> FindTrackedAsync(CrmDbContext dbContext, Guid id, DataScope scope, CancellationToken cancellationToken) =>
        ContactScopeFilter.Apply(dbContext.Contacts.Where(contact => contact.Id == id), scope)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>A contact row the caller may read, or null when missing, deleted or out of scope.</summary>
    public static Task<ContactRow?> FindRowAsync(CrmDbContext dbContext, Guid id, DataScope scope, CancellationToken cancellationToken) =>
        ContactScopeFilter.Apply(dbContext.Contacts.AsNoTracking().Where(contact => contact.Id == id), scope)
            .Select(ContactViews.RowProjection)
            .FirstOrDefaultAsync(cancellationToken);
}
