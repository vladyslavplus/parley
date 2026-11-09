namespace Parley.Identity.Contracts.Users;

/// <summary>
/// How another module sees one person: the display name, the email and whether the membership in the
/// organization is active. Blank values read as absent.
/// </summary>
/// <param name="UserId">User identifier.</param>
/// <param name="DisplayName">Display name, or null when the user has none.</param>
/// <param name="Email">Email address, or null when unknown.</param>
/// <param name="IsActive">True while the membership in the organization is active.</param>
public sealed record UserDirectoryEntry(Guid UserId, string? DisplayName, string? Email, bool IsActive)
{
    /// <summary>Builds an entry with surrounding whitespace removed and blank values replaced by null.</summary>
    public static UserDirectoryEntry Create(Guid userId, string? displayName, string? email, bool isActive) => new(
        userId,
        string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
        string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
        isActive);
}
