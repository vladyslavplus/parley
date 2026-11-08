using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Options.Authorization;
using Parley.Identity.Persistence;

namespace Parley.Identity.Authorization.Permissions;

/// <summary>
/// Cached permission sets by role, backed by HybridCache. Entries are tagged by
/// role, so <see cref="InvalidateAsync"/> removes one role's set without touching
/// the others.
/// </summary>
/// <remarks>
/// The cached value is an array, not an interface, because HybridCache serializes
/// values with System.Text.Json and cannot create interface types on read.
/// </remarks>
public sealed class RolePermissionCache(
    HybridCache cache,
    IdentityDbContext dbContext,
    IOptions<AuthorizationOptions> options)
{
    private readonly HybridCacheEntryOptions _entryOptions = new()
    {
        Expiration = options.Value.RolePermissionCacheLifetime,
        LocalCacheExpiration = options.Value.RolePermissionCacheLifetime,
    };

    /// <summary>Returns the grants of a role, loading them on a miss.</summary>
    public ValueTask<RolePermissionGrant[]> GetAsync(Guid roleId, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            AuthorizationCacheKeys.RolePermissions(roleId),
            roleId,
            (state, token) => LoadAsync(state, token),
            _entryOptions,
            [AuthorizationCacheKeys.RolePermissionsTag(roleId)],
            cancellationToken);

    /// <summary>Removes the cached grants of one role.</summary>
    public ValueTask InvalidateAsync(Guid roleId, CancellationToken cancellationToken) =>
        cache.RemoveByTagAsync(AuthorizationCacheKeys.RolePermissionsTag(roleId), cancellationToken);

    private async ValueTask<RolePermissionGrant[]> LoadAsync(Guid roleId, CancellationToken cancellationToken) =>
        await dbContext.RolePermissions
            .AsNoTracking()
            .Where(grant => grant.RoleId == roleId)
            .Select(grant => new RolePermissionGrant(grant.Permission, grant.Scope))
            .ToArrayAsync(cancellationToken);
}
