using Parley.Identity.Contracts.Authorization;

namespace Parley.Identity.Authorization.Scope;

/// <summary>
/// Maps a stored grant scope to a visibility level. Pure rules, so the mapping
/// for every system role can be tested without a database.
/// </summary>
public static class DataScopeRules
{
    /// <summary>
    /// A null scope is an organization-level capability and sees every record. A
    /// team scope without a team falls back to own scope. An unknown scope text sees
    /// nothing, so a bad row fails closed.
    /// </summary>
    public static DataScopeLevel Resolve(string? storedScope, bool userHasTeam) => storedScope switch
    {
        null => DataScopeLevel.Organization,
        RoleScopeNames.Organization => DataScopeLevel.Organization,
        RoleScopeNames.Team => userHasTeam ? DataScopeLevel.Team : DataScopeLevel.Own,
        RoleScopeNames.Own => DataScopeLevel.Own,
        _ => DataScopeLevel.None,
    };
}
