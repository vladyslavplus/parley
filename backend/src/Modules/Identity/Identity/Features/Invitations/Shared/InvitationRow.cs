namespace Parley.Identity.Features.Invitations.Shared;

/// <summary>A pending invitation with its role code and the sort keys the invitation list orders by.</summary>
internal sealed record InvitationRow(Guid Id, string Email, string RoleCode, Guid? TeamId, DateTime ExpiresAt, DateTime CreatedAt);
