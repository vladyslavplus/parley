namespace Parley.Identity.Features.Members.Shared;

/// <summary>One member row as the member list reads it: the membership with its user's contact fields and role code.</summary>
internal sealed record MemberRow(
    Guid Id,
    Guid UserId,
    string? Email,
    string? DisplayName,
    string RoleCode,
    Guid? TeamId,
    string Status,
    DateTime JoinedAt);
