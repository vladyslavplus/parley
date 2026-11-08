using Microsoft.EntityFrameworkCore;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Domain.Memberships;
using Parley.Identity.Persistence;

namespace Parley.Identity.Features.Members.Shared;

/// <summary>
/// Shared membership rules: system role lookup and the last-administrator guard.
/// Changes that can remove the last administrator lock the organization row first,
/// so two concurrent demotions cannot both see another administrator.
/// </summary>
public static class MemberRules
{
    public static async Task<Guid?> FindSystemRoleIdAsync(IdentityDbContext dbContext, string roleCode, CancellationToken cancellationToken) =>
        await dbContext.Roles
            .AsNoTracking()
            .Where(role => role.OrganizationId == null && role.Code == roleCode)
            .Select(role => (Guid?)role.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Takes a row lock on the current organization until the surrounding transaction ends.
    /// The organization row is not changed; the update only serializes admin changes.
    /// </summary>
    /// <summary>
    /// Serializes the member rules of one organization until the surrounding transaction ends.
    /// The lock is an advisory transaction lock keyed on the organization, so the organization
    /// row keeps its version and a concurrent settings save is not turned into a conflict.
    /// </summary>
    public static Task<int> LockOrganizationAsync(IdentityDbContext dbContext, Guid organizationId, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({organizationId.ToString("D")}, 0))",
            cancellationToken);

    /// <summary>True when the membership is an active administrator and no other active administrator exists.</summary>
    public static async Task<bool> IsLastActiveAdminAsync(IdentityDbContext dbContext, Membership membership, CancellationToken cancellationToken)
    {
        if (membership.RoleId != SystemRoleSeed.AdminRoleId || membership.Status != MembershipStatuses.Active)
        {
            return false;
        }

        var otherAdmins = await dbContext.Memberships
            .CountAsync(other => other.Id != membership.Id
                && other.RoleId == SystemRoleSeed.AdminRoleId
                && other.Status == MembershipStatuses.Active, cancellationToken);
        return otherAdmins == 0;
    }
}
