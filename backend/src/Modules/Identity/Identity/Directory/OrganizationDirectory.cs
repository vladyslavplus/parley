using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Organizations;
using Parley.Identity.Persistence;

namespace Parley.Identity.Directory;

/// <summary>
/// Lists every organization with its time zone in one query. The tenant filter is bypassed because the reader is a
/// platform job helper; the only columns read are the id and the time zone.
/// </summary>
public sealed class OrganizationDirectory(IdentityDbContext dbContext) : IOrganizationDirectory
{
    public async Task<IReadOnlyList<OrganizationTimeZone>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Organizations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(organization => organization.Id)
            .Select(organization => new OrganizationTimeZone(organization.Id, organization.TimeZone))
            .ToListAsync(cancellationToken);
}
