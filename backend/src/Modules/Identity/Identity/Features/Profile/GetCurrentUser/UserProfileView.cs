namespace Parley.Identity.Features.Profile.GetCurrentUser;

/// <summary>The personal profile of the signed-in user.</summary>
public sealed record UserProfileView(Guid Id, string Email, string? DisplayName, string Locale, string TimeZone, string Theme);
