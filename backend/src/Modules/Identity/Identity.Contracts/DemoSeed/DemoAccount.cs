namespace Parley.Identity.Contracts.DemoSeed;

/// <summary>One demo account. <see cref="Password"/> is the shared development password; it is never logged.</summary>
/// <param name="UserId">Identifier of the account.</param>
/// <param name="Email">Sign-in address; the demo accounts use the reserved demo domain, the local test account its own domain.</param>
/// <param name="DisplayName">Name shown in the interface.</param>
/// <param name="RoleCode">System role code of the membership.</param>
/// <param name="TeamName">Team of the membership; null without a team.</param>
/// <param name="Password">The shared development password passed to the seed.</param>
public sealed record DemoAccount(
    Guid UserId,
    string Email,
    string DisplayName,
    string RoleCode,
    string? TeamName,
    string? Password);
