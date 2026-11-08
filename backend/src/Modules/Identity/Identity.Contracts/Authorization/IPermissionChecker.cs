namespace Parley.Identity.Contracts.Authorization;

/// <summary>
/// Server-side permission decisions. The membership (role and status) is read on
/// every call, so deactivation and role changes apply on the next request. Role
/// permission sets come from a cache that <see cref="InvalidateRolePermissionsAsync"/>
/// clears when the grants of a role change.
/// </summary>
public interface IPermissionChecker
{
    /// <summary>True when the user has an active membership in the organization and its role grants the permission.</summary>
    Task<bool> HasPermissionAsync(Guid userId, Guid organizationId, string permission, CancellationToken cancellationToken = default);

    /// <summary>True when the user has an active membership in the organization.</summary>
    Task<bool> IsActiveMemberAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>Drops the cached permission set of a role. Call after its grants change.</summary>
    Task InvalidateRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
}
