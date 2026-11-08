namespace Parley.Identity.Features.Members.Shared;

/// <summary>A member of the current organization as the administrator sees it.</summary>
public sealed record MemberView(
    Guid Id,
    Guid UserId,
    string? Email,
    string? DisplayName,
    string RoleCode,
    Guid? TeamId,
    string Status,
    DateTime JoinedAt);
