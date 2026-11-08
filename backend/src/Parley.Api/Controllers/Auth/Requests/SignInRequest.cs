namespace Parley.Api.Controllers.Auth.Requests;


/// <summary>Body of <c>login</c>.</summary>
public sealed record SignInRequest(string? Email, string? Password);
