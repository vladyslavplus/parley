namespace Parley.Identity.Domain.Sessions;

/// <summary>
/// One issued refresh token. Only the SHA-256 hash of the opaque value is stored.
/// Rotation links a token to its successor through <see cref="ReplacedById"/>,
/// and every token of one sign-in session shares a <see cref="FamilyId"/>, so
/// reuse of a rotated token can revoke the whole session.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Shared by every token issued in one sign-in session.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>SHA-256 of the opaque token. Unique, never the token itself.</summary>
    public byte[] TokenHash { get; set; } = [];

    /// <summary>Organization the session acts for; copied into each new access token.</summary>
    public Guid OrganizationId { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the token is rotated, revoked by logout, or revoked with its family.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Successor created by rotation. Empty for revocations that did not rotate.</summary>
    public Guid? ReplacedById { get; set; }

    public DateTime CreatedAt { get; set; }
}
