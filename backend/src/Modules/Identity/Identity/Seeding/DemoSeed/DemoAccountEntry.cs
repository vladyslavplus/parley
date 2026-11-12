namespace Parley.Identity.Seeding.DemoSeed;

/// <summary>One account of the demo catalogue.</summary>
/// <param name="Email">Full sign-in address.</param>
/// <param name="DisplayName">Name shown in the interface.</param>
/// <param name="RoleId">System role of the membership.</param>
/// <param name="RoleCode">System role code, returned to the host for its output.</param>
/// <param name="TeamName">Team name; null for organization-level accounts.</param>
public sealed record DemoAccountEntry(
    string Email,
    string DisplayName,
    Guid RoleId,
    string RoleCode,
    string? TeamName);
