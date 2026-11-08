namespace Parley.Api.Controllers.Identity.Requests;

/// <summary>Body of <c>POST /invitations/accept</c>.</summary>
public sealed record AcceptInvitationRequest(string? Token, string? Password);
