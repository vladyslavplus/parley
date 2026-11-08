namespace Parley.Api.RateLimiting;

/// <summary>Policy names for the per-endpoint authentication limits.</summary>
public static class AuthRateLimitPolicies
{
    public const string Register = "auth-register";

    public const string Login = "auth-login";

    public const string Refresh = "auth-refresh";

    public const string ConfirmEmail = "auth-confirm-email";

    public const string ResendConfirmation = "auth-resend-confirmation";

    public const string ForgotPassword = "auth-forgot-password";

    public const string ResetPassword = "auth-reset-password";

    /// <summary>Invitation creation by an administrator, per client address.</summary>
    public const string InvitationCreate = "invitation-create";

    /// <summary>Anonymous invitation acceptance, per client address.</summary>
    public const string InvitationAccept = "invitation-accept";

    /// <summary>Partition used when the client address is unknown.</summary>
    public const string UnknownClientAddress = "unknown";
}
