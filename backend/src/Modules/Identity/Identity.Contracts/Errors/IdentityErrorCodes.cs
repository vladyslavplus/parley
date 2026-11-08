namespace Parley.Identity.Contracts.Errors;

/// <summary>
/// Stable error codes for account and sign-in failures. Each code has an
/// English and a Ukrainian text in the shared error resources.
/// </summary>
public static class IdentityErrorCodes
{
    public const string UserEmailTaken = "identity.user.email_taken";
    public const string UserNotFound = "identity.user.not_found";
    public const string UserPasswordInvalid = "identity.user.password_invalid";
    public const string AuthInvalidCredentials = "identity.auth.invalid_credentials";
    public const string AuthEmailNotConfirmed = "identity.auth.email_not_confirmed";
    public const string AuthTokenInvalid = "identity.auth.token_invalid";
    public const string AuthRefreshTokenInvalid = "identity.auth.refresh_token_invalid";
    public const string AuthRefreshTokenReused = "identity.auth.refresh_token_reused";
    public const string AuthRateLimited = "identity.auth.rate_limited";
    public const string PermissionDenied = "identity.permission.denied";
    public const string MemberNotFound = "identity.member.not_found";
    public const string MemberLastAdmin = "identity.member.last_admin";
    public const string MemberSelfDeactivation = "identity.member.self_deactivation";
    public const string MemberAlreadyMember = "identity.member.already_member";
    public const string MemberRoleInvalid = "identity.member.role_invalid";
    public const string InvitationNotFound = "identity.invitation.not_found";
    public const string InvitationTokenInvalid = "identity.invitation.token_invalid";
    public const string InvitationAlreadyPending = "identity.invitation.already_pending";
    public const string InvitationNotPending = "identity.invitation.not_pending";
    public const string TeamNotFound = "identity.team.not_found";
    public const string TeamNameTaken = "identity.team.name_taken";
    public const string OrganizationNotFound = "identity.organization.not_found";
}
