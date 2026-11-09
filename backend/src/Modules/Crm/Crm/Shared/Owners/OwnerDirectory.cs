using Parley.Identity.Contracts.Users;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Shared.Owners;

/// <summary>
/// Resolves the owners of a page of records in one call to the user directory. An owner outside the
/// organization, or with no name, is absent from the result, and the view shows the field as empty.
/// </summary>
public sealed class OwnerDirectory(IUserDirectory users, ITenantContext tenantContext)
{
    public async Task<IReadOnlyDictionary<Guid, UserDirectoryEntry>> ReadAsync(IEnumerable<Guid?> ownerIds, CancellationToken cancellationToken)
    {
        var ids = ownerIds.OfType<Guid>().Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, UserDirectoryEntry>();
        }

        return await users.FindMembersAsync(tenantContext.OrganizationId, ids, cancellationToken);
    }

    /// <summary>
    /// Active members whose name or email contains <paramref name="term"/>, one directory call. Null
    /// <paramref name="ownerIds"/> means no owner limit; an empty collection means no owner at all.
    /// </summary>
    public Task<IReadOnlyList<UserDirectoryEntry>> SearchAsync(
        string? term,
        IReadOnlyCollection<Guid>? ownerIds,
        int limit,
        CancellationToken cancellationToken) =>
        users.SearchActiveMembersAsync(tenantContext.OrganizationId, term, ownerIds, limit, cancellationToken);

    /// <summary>The directory entry of an owner, or null when the record is unowned or the owner is unknown.</summary>
    public static UserDirectoryEntry? Find(IReadOnlyDictionary<Guid, UserDirectoryEntry> owners, Guid? ownerUserId) =>
        ownerUserId is { } id && owners.TryGetValue(id, out var entry) ? entry : null;
}
