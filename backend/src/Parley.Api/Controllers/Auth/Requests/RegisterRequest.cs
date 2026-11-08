namespace Parley.Api.Controllers.Auth.Requests;


/// <summary>Body of <c>register</c>. Properties are nullable so the handler, not model binding, reports missing values.</summary>
public sealed record RegisterRequest(string? Email, string? Password, string? OrganizationName);
