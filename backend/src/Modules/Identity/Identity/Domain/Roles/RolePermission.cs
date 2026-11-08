namespace Parley.Identity.Domain.Roles;

/// <summary>
/// One permission granted to a role. Scope is stored on the grant: null for
/// organization-level capabilities, otherwise own, team or org.
/// </summary>
public sealed class RolePermission
{
    public Guid RoleId { get; set; }

    public string Permission { get; set; } = string.Empty;

    public string? Scope { get; set; }
}
