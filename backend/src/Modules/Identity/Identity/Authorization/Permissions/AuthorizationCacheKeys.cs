namespace Parley.Identity.Authorization.Permissions;

/// <summary>
/// HybridCache keys and tags for authorization data. The tag of a role groups its
/// permission set, so changing the role's grants removes exactly that entry.
/// </summary>
public static class AuthorizationCacheKeys
{
    private const string RolePermissionsPrefix = "identity:role-permissions:";

    /// <summary>Cache key of the permission set of one role.</summary>
    public static string RolePermissions(Guid roleId) => RolePermissionsPrefix + roleId.ToString("D");

    /// <summary>Tag shared by the cache entry of one role.</summary>
    public static string RolePermissionsTag(Guid roleId) => RolePermissions(roleId);
}
