namespace Parley.Api.Controllers.Auth.Requests;


/// <summary>Body of endpoints that take only an address.</summary>
public sealed record EmailRequest(string? Email);
