namespace Parley.Identity.Contracts.Authorization;

/// <summary>
/// Stored scope names on a role-permission grant. A null scope means an
/// organization-level capability with no record scope.
/// </summary>
public static class RoleScopeNames
{
    /// <summary>Records the user owns; tasks use the assignee.</summary>
    public const string Own = "own";

    /// <summary>Records owned by the user or by a member of the user's team.</summary>
    public const string Team = "team";

    /// <summary>Every record of the organization.</summary>
    public const string Organization = "org";
}
