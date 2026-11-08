namespace Parley.Identity.Features.Profile.GetCurrentUser;

/// <summary>
/// Everything the interface needs to show and hide features: the profile, the active
/// organization, the role, the team, and the permissions with their record scope. The
/// server still checks every request; this view only shapes the UI.
/// </summary>
public sealed record CurrentUserView(
    UserProfileView User,
    CurrentOrganizationView Organization,
    string RoleCode,
    Guid? TeamId,
    IReadOnlyList<PermissionGrantView> Permissions);
