using Microsoft.EntityFrameworkCore;
using Parley.Identity.Authorization.Permissions;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Persistence;

namespace Parley.Identity.Authorization.Scope;

/// <summary>
/// Resolves record visibility from the active membership and the role grant. Team
/// members are listed through the membership table, so team scope follows team
/// membership changes on the next request.
/// </summary>
public sealed class UserScopeReader(IdentityDbContext dbContext, RolePermissionCache rolePermissions) : IUserScopeReader
{
    public async Task<DataScope> ReadAsync(Guid userId, Guid organizationId, string permission, CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.Memberships
            .AsNoTracking()
            .Where(row => row.OrganizationId == organizationId
                && row.UserId == userId
                && row.Status == MembershipStatuses.Active)
            .Select(row => new { row.RoleId, row.TeamId })
            .FirstOrDefaultAsync(cancellationToken);
        if (membership is null)
        {
            return DataScope.Nothing;
        }

        var grants = await rolePermissions.GetAsync(membership.RoleId, cancellationToken);
        var grant = Array.Find(grants, candidate => string.Equals(candidate.Permission, permission, StringComparison.Ordinal));
        if (grant is null)
        {
            return DataScope.Nothing;
        }

        var level = DataScopeRules.Resolve(grant.Scope, membership.TeamId is not null);
        return level switch
        {
            DataScopeLevel.Organization => new DataScope(level, null),
            DataScopeLevel.Own => new DataScope(level, [userId]),
            DataScopeLevel.Team => new DataScope(level, await ReadTeamOwnerIdsAsync(membership.TeamId!.Value, organizationId, cancellationToken)),
            _ => DataScope.Nothing,
        };
    }

    private async Task<Guid[]> ReadTeamOwnerIdsAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken) =>
        await dbContext.Memberships
            .AsNoTracking()
            .Where(row => row.OrganizationId == organizationId
                && row.TeamId == teamId
                && row.Status == MembershipStatuses.Active)
            .Select(row => row.UserId)
            .ToArrayAsync(cancellationToken);
}
