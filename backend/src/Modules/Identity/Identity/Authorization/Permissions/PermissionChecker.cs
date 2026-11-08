using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Persistence;

namespace Parley.Identity.Authorization.Permissions;

/// <summary>
/// Permission decisions. The membership is read per call (organization and user
/// primary key, tenant filter applied), so a deactivated member or a changed role is
/// rejected on the next request even with an old token. The role's permission set
/// comes from <see cref="RolePermissionCache"/>.
/// </summary>
public sealed class PermissionChecker(IdentityDbContext dbContext, RolePermissionCache rolePermissions) : IPermissionChecker
{
    public async Task<bool> HasPermissionAsync(Guid userId, Guid organizationId, string permission, CancellationToken cancellationToken = default)
    {
        var membership = await ReadActiveMembershipAsync(userId, organizationId, cancellationToken);
        if (membership is null)
        {
            return false;
        }

        var grants = await rolePermissions.GetAsync(membership.RoleId, cancellationToken);
        return Array.Exists(grants, grant => string.Equals(grant.Permission, permission, StringComparison.Ordinal));
    }

    public async Task<bool> IsActiveMemberAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default) =>
        await ReadActiveMembershipAsync(userId, organizationId, cancellationToken) is not null;

    public async Task InvalidateRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await rolePermissions.InvalidateAsync(roleId, cancellationToken);

    private async Task<MembershipState?> ReadActiveMembershipAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken)
    {
        return await dbContext.Memberships
            .AsNoTracking()
            .Where(membership => membership.OrganizationId == organizationId
                && membership.UserId == userId
                && membership.Status == MembershipStatuses.Active)
            .Select(membership => new MembershipState(membership.RoleId, membership.TeamId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>The fields of a membership that authorization needs.</summary>
    internal sealed record MembershipState(Guid RoleId, Guid? TeamId);
}
