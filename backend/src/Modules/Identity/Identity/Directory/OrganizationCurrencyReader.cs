using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Organizations;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Directory;

/// <summary>Reads the currency of the current tenant's organization in one statement.</summary>
public sealed class OrganizationCurrencyReader(IdentityDbContext dbContext, ITenantContext tenantContext)
    : IOrganizationCurrencyReader
{
    public Task<string?> ReadAsync(CancellationToken cancellationToken) =>
        dbContext.Organizations.AsNoTracking()
            .Where(organization => organization.Id == tenantContext.OrganizationId)
            .Select(organization => organization.Currency)
            .FirstOrDefaultAsync(cancellationToken);
}
