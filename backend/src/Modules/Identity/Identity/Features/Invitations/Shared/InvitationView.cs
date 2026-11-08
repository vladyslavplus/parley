namespace Parley.Identity.Features.Invitations.Shared;

/// <summary>A pending invitation as the administrator sees it.</summary>
public sealed record InvitationView(Guid Id, string Email, string RoleCode, Guid? TeamId, DateTime ExpiresAt);
