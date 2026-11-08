using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Features.Companies.Shared;

/// <summary>Scoped company lookups shared by the company handlers.</summary>
internal static class CompanyLookup
{
    /// <summary>A tracked company the caller may change, or null when missing, deleted or out of scope.</summary>
    public static Task<Company?> FindTrackedAsync(CrmDbContext dbContext, Guid id, DataScope scope, CancellationToken cancellationToken) =>
        CompanyScopeFilter.Apply(dbContext.Companies.Where(company => company.Id == id), scope)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>A company row the caller may read, or null when missing, deleted or out of scope.</summary>
    public static Task<CompanyRow?> FindRowAsync(CrmDbContext dbContext, Guid id, DataScope scope, CancellationToken cancellationToken) =>
        CompanyScopeFilter.Apply(dbContext.Companies.AsNoTracking().Where(company => company.Id == id), scope)
            .Select(CompanyViews.RowProjection)
            .FirstOrDefaultAsync(cancellationToken);
}
