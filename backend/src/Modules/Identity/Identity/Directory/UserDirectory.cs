using Microsoft.EntityFrameworkCore;
using Parley.Identity.Contracts.Memberships;
using Parley.Identity.Contracts.Users;
using Parley.Identity.Persistence;
using Parley.SharedKernel.Search;

namespace Parley.Identity.Directory;

/// <summary>
/// Reads the directory in one query each: memberships of the organization joined to their users. The
/// organization filter is applied on the membership, so a user with no membership there is absent.
/// </summary>
public sealed class UserDirectory(IdentityDbContext dbContext) : IUserDirectory
{
    public async Task<IReadOnlyDictionary<Guid, UserDirectoryEntry>> FindMembersAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, UserDirectoryEntry>();
        }

        var ids = userIds.Distinct().ToList();
        var rows = await (
            from membership in dbContext.Memberships.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
            where membership.OrganizationId == organizationId && ids.Contains(membership.UserId)
            select new { user.Id, user.DisplayName, user.Email, membership.Status })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row.Id,
            row => UserDirectoryEntry.Create(row.Id, row.DisplayName, row.Email, row.Status == MembershipStatuses.Active));
    }

    public async Task<IReadOnlyList<UserDirectoryEntry>> SearchActiveMembersAsync(
        Guid organizationId,
        string? term,
        IReadOnlyCollection<Guid>? userIds,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (userIds is { Count: 0 })
        {
            return [];
        }

        var members = (
            from membership in dbContext.Memberships.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
            where membership.OrganizationId == organizationId && membership.Status == MembershipStatuses.Active
            select new { user.Id, user.DisplayName, user.Email });

        if (userIds is not null)
        {
            var ids = userIds.ToList();
            members = members.Where(member => ids.Contains(member.Id));
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = LikePattern.Contains(term);
            members = members.Where(member =>
                (member.DisplayName != null && EF.Functions.ILike(member.DisplayName, pattern))
                || (member.Email != null && EF.Functions.ILike(member.Email, pattern)));
        }

        var rows = await members
            .OrderBy(member => member.DisplayName)
            .ThenBy(member => member.Email)
            .ThenBy(member => member.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => UserDirectoryEntry.Create(row.Id, row.DisplayName, row.Email, isActive: true))
            .ToArray();
    }
}
