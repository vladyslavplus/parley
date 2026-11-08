namespace Parley.Identity.Options.Authorization;

/// <summary>
/// Authorization cache settings. Bind to the <c>Authorization</c> section; validated on start.
/// </summary>
public sealed class AuthorizationOptions
{
    public const string SectionName = "Authorization";

    /// <summary>Default lifetime of a cached role permission set before it is reloaded.</summary>
    private static readonly TimeSpan DefaultRolePermissionCacheLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long a role permission set stays cached. Role grant changes call the
    /// invalidation method immediately, so this only bounds staleness from
    /// changes made outside the API.
    /// </summary>
    public TimeSpan RolePermissionCacheLifetime { get; set; } = DefaultRolePermissionCacheLifetime;
}
