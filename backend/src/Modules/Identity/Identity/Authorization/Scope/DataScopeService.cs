using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Identity.Authorization.Scope;

/// <summary>
/// Per-request record visibility. Reads the current user and organization from the
/// request scope and memoizes each permission, so a request that checks the same
/// permission twice reads the membership and grant once. Scoped, never shared.
/// </summary>
public sealed class DataScopeService(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    IUserScopeReader scopeReader) : IDataScope
{
    private readonly Dictionary<string, DataScope> _resolved = new(StringComparer.Ordinal);

    public async ValueTask<DataScope> ForPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (_resolved.TryGetValue(permission, out var cached))
        {
            return cached;
        }

        var userId = currentUser.UserId;
        if (userId is null)
        {
            return DataScope.Nothing;
        }

        var scope = await scopeReader.ReadAsync(userId.Value, tenantContext.OrganizationId, permission, cancellationToken);
        _resolved[permission] = scope;
        return scope;
    }
}
