using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Identity.Domain.Invitations;

/// <summary>
/// An invitation for an address to join an organization. Only the hash of the
/// invitation token is stored.
/// </summary>
public sealed class Invitation : BusinessEntity
{
    public Invitation()
    {
    }

    public Invitation(Guid organizationId) : base(organizationId)
    {
    }

    public string EmailNormalized { get; set; } = string.Empty;

    public Guid RoleId { get; set; }

    public Guid? TeamId { get; set; }

    /// <summary>SHA-256 of the invitation token. Unique, never the token itself.</summary>
    public byte[] TokenHash { get; set; } = [];

    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid InvitedBy { get; set; }
}
