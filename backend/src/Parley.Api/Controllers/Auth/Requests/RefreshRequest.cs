namespace Parley.Api.Controllers.Auth.Requests;


/// <summary>Body of <c>refresh</c>, <c>logout</c> and organization switch.</summary>
public sealed record RefreshRequest(string? RefreshToken);
