namespace Parley.Identity.Authorization.Seed;

/// <summary>One stored grant of a role, as cached.</summary>
/// <param name="Permission">Permission string.</param>
/// <param name="Scope">Stored scope, or null for organization-level grants.</param>
public sealed record RolePermissionGrant(string Permission, string? Scope);
