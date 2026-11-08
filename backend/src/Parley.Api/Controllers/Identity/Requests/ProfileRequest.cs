namespace Parley.Api.Controllers.Identity.Requests;

/// <summary>Body of <c>PUT /me/profile</c>.</summary>
public sealed record ProfileRequest(string? DisplayName, string? Locale, string? TimeZone, string? Theme);
