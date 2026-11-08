namespace Parley.Api.Controllers.Auth.Requests;


/// <summary>Body of <c>reset-password</c>.</summary>
public sealed record ResetPasswordRequest(Guid? UserId, string? Token, string? NewPassword);
