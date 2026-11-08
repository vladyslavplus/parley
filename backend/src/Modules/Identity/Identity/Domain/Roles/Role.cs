namespace Parley.Identity.Domain.Roles;

/// <summary>
/// A named set of permission grants. System roles have no organization and are
/// seeded; custom roles (a later feature) carry the organization id.
/// </summary>
public sealed class Role
{
    public Guid Id { get; set; }

    /// <summary>Null for system roles.</summary>
    public Guid? OrganizationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public bool IsSystem { get; set; }
}
