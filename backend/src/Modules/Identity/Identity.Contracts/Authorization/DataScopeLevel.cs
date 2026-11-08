namespace Parley.Identity.Contracts.Authorization;

/// <summary>
/// Which records the current user may see for one permission.
/// </summary>
public enum DataScopeLevel
{
    /// <summary>No grant: nothing is visible.</summary>
    None = 0,

    /// <summary>Records whose owner is the current user.</summary>
    Own,

    /// <summary>Records whose owner is the current user or a member of their team.</summary>
    Team,

    /// <summary>Every record of the organization, including unowned ones.</summary>
    Organization,
}
