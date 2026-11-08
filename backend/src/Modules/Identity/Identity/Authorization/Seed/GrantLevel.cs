namespace Parley.Identity.Authorization.Seed;

/// <summary>
/// Scope of one grant in the permission matrix. <see cref="NotGranted"/> means no
/// row; <see cref="OrganizationLevel"/> means a row with a null scope.
/// </summary>
public enum GrantLevel
{
    NotGranted = 0,
    OrganizationLevel,
    Own,
    Team,
    Organization,
}
