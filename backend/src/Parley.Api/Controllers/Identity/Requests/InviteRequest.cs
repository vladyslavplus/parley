namespace Parley.Api.Controllers.Identity.Requests;

/// <summary>Body of <c>POST /invitations</c>.</summary>
public sealed record InviteRequest(string? Email, string? RoleCode, Guid? TeamId);
