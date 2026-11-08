namespace Parley.Identity.Features.Profile.GetCurrentUser;

/// <summary>The organization the session acts for, as the signed-in user sees it.</summary>
public sealed record CurrentOrganizationView(Guid Id, string Name, string Slug, string TimeZone, string DefaultLocale, string Currency);
