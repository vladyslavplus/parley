namespace Parley.Identity.Contracts.DemoSeed;

/// <summary>One demo account. <see cref="Password"/> is set only for accounts this run created; it is never logged.</summary>
/// <param name="UserId">Identifier of the account.</param>
/// <param name="Email">Sign-in address on the reserved demo domain.</param>
/// <param name="DisplayName">Name shown in the interface.</param>
/// <param name="RoleCode">System role code of the membership.</param>
/// <param name="TeamName">Team of the membership; null without a team.</param>
/// <param name="Password">Generated password; null when the account already existed.</param>
public sealed record DemoAccount(
    Guid UserId,
    string Email,
    string DisplayName,
    string RoleCode,
    string? TeamName,
    string? Password);
