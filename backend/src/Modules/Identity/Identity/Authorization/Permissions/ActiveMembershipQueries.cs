using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Persistence;

namespace Parley.Identity.Authorization.Permissions;

/// <summary>
/// Membership reads for anonymous flows (sign-in and refresh), where no organization
/// is active yet. The organization and user are explicit predicates, so the tenant
/// filter is not needed; these are the only tenant-less reads of the membership table.
/// </summary>
public static class ActiveMembershipQueries
{
    /// <summary>True when the user has an active membership in the organization.</summary>
    public static Task<bool> IsActiveAsync(IdentityDbContext dbContext, Guid userId, Guid organizationId, CancellationToken cancellationToken) =>
        dbContext.Memberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(membership => membership.UserId == userId
                && membership.OrganizationId == organizationId
                && membership.Status == MembershipStatuses.Active, cancellationToken);

    /// <summary>
    /// Chooses the organization a new session acts for: the last active one while the
    /// user is still a member there, otherwise the oldest active membership. Returns
    /// null when the user has no active membership at all.
    /// </summary>
    public static async Task<Guid?> ResolveSignInOrganizationAsync(
        IdentityDbContext dbContext,
        Guid userId,
        Guid? lastActiveOrganizationId,
        CancellationToken cancellationToken)
    {
        if (lastActiveOrganizationId is { } lastActive
            && await IsActiveAsync(dbContext, userId, lastActive, cancellationToken))
        {
            return lastActive;
        }

        return await dbContext.Memberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(membership => membership.UserId == userId && membership.Status == MembershipStatuses.Active)
            .OrderBy(membership => membership.JoinedAt)
            .Select(membership => (Guid?)membership.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
