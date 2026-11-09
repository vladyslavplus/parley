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

    /// <summary>
    /// A contact row the caller may read, or null when missing, deleted or out of scope. The company name is
    /// limited to the companies the caller may read.
    /// </summary>
    public static async Task<ContactRow?> FindRowAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        Guid id,
        DataScope scope,
        CancellationToken cancellationToken)
    {
        var companyScope = await dataScope.ForPermissionAsync(Permissions.CompaniesRead, cancellationToken);
        var visibleCompanies = CompanyScopeFilter.Apply(dbContext.Companies.AsNoTracking(), companyScope);
        return await ContactScopeFilter.Apply(dbContext.Contacts.AsNoTracking().Where(contact => contact.Id == id), scope)
            .Select(ContactViews.RowProjectionFor(visibleCompanies))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
