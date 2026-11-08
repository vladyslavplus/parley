namespace Parley.Identity.Features.Profile.GetCurrentUser;

/// <summary>
/// One permission of the current role with the record scope it applies to. A null scope
/// is an organization-level capability with no record scope.
/// </summary>
/// <param name="Permission">Permission string, for example <c>contacts.read</c>.</param>
/// <param name="Scope">Record scope: <c>own</c>, <c>team</c>, <c>org</c>, or null.</param>
public sealed record PermissionGrantView(string Permission, string? Scope);
