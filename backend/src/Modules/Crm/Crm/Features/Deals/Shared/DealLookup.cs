using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Deals;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Features.Deals.Shared;

/// <summary>Scoped deal lookups shared by the deal handlers.</summary>
internal static class DealLookup
{
    /// <summary>A tracked deal the caller may change, or null when missing, deleted or out of scope.</summary>
    public static Task<Deal?> FindTrackedAsync(
        CrmDbContext dbContext,
        Guid id,
        DataScope scope,
        CancellationToken cancellationToken,
        bool includeDeleted = false) =>
        DealScopeFilter.Apply(dbContext.Deals.Where(deal => deal.Id == id), scope, includeDeleted).FirstOrDefaultAsync(cancellationToken);

    /// <summary>A deal row the caller may read, or null when missing, deleted or out of scope.</summary>
    public static async Task<DealRow?> FindRowAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        Guid id,
        DataScope scope,
        CancellationToken cancellationToken)
    {
        var visibleContacts = await VisibleContactsAsync(dbContext, dataScope, cancellationToken);
        var visibleCompanies = await VisibleCompaniesAsync(dbContext, dataScope, cancellationToken);
        return await DealScopeFilter.Apply(dbContext.Deals.AsNoTracking().Where(deal => deal.Id == id), scope)
            .Select(DealViews.RowProjectionFor(visibleContacts, visibleCompanies, dbContext.PipelineStages.AsNoTracking()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Live deals the caller may read; a link to any other deal is reported as missing.</summary>
    public static async Task<IQueryable<Deal>> VisibleDealsAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.DealsRead, cancellationToken);
        return DealScopeFilter.Apply(dbContext.Deals.AsNoTracking(), scope);
    }

    /// <summary>Contacts the caller may read; a link to any other contact is reported as missing.</summary>
    public static async Task<IQueryable<Contact>> VisibleContactsAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        return ContactScopeFilter.Apply(dbContext.Contacts.AsNoTracking(), scope);
    }

    /// <summary>Companies the caller may read; a link to any other company is reported as missing.</summary>
    public static async Task<IQueryable<Company>> VisibleCompaniesAsync(
        CrmDbContext dbContext,
        IDataScope dataScope,
        CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.CompaniesRead, cancellationToken);
        return CompanyScopeFilter.Apply(dbContext.Companies.AsNoTracking(), scope);
    }
}
