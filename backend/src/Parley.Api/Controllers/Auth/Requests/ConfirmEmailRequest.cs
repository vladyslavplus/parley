namespace Parley.Api.Controllers.Auth.Requests;


/// <summary>Body of <c>confirm-email</c>.</summary>
public sealed record ConfirmEmailRequest(Guid? UserId, string? Token);
