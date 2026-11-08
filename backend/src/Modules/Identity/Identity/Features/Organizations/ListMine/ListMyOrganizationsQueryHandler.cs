using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Features.Organizations.ListMine;

public sealed class ListMyOrganizationsQueryHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : IRequestHandler<ListMyOrganizationsQuery, Result<IReadOnlyList<MyOrganizationView>>>
{
    public async ValueTask<Result<IReadOnlyList<MyOrganizationView>>> Handle(ListMyOrganizationsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var currentOrganizationId = tenantContext.OrganizationId;

        // Memberships are tenant-filtered by default; this query spans the user's organizations on purpose,
        // and the user filter below keeps it to the caller's own memberships.
        var organizations = await (
                from membership in dbContext.Memberships.IgnoreQueryFilters().AsNoTracking()
                join organization in dbContext.Organizations.AsNoTracking() on membership.OrganizationId equals organization.Id
                join role in dbContext.Roles.AsNoTracking() on membership.RoleId equals role.Id
                where membership.UserId == userId && membership.Status == MembershipStatuses.Active
                orderby organization.Name, organization.Id
                select new MyOrganizationView(organization.Id, organization.Name, role.Code, organization.Id == currentOrganizationId))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<MyOrganizationView>>.Success(organizations);
    }
}
