using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Authorization.Scope;

/// <summary>
/// Decides whether a record may be assigned to an owner. The owner must be inside the
/// writer's scope: the writer for own scope, the team for team scope, any active member
/// for organization scope.
/// </summary>
public static class OwnerRules
{
    public static async Task<bool> IsAllowedAsync(
        Guid ownerUserId,
        DataScope scope,
        Guid organizationId,
        IPermissionChecker permissionChecker,
        CancellationToken cancellationToken)
    {
        return scope.Level switch
        {
            DataScopeLevel.Organization => await permissionChecker.IsActiveMemberAsync(ownerUserId, organizationId, cancellationToken),
            DataScopeLevel.Own or DataScopeLevel.Team => scope.OwnerUserIds?.Contains(ownerUserId) == true,
            _ => false,
        };
    }
}
