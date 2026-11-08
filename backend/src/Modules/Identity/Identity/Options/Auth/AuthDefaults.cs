namespace Parley.Identity.Options.Auth;

/// <summary>
/// Defaults and limits for <see cref="AuthOptions"/>. None of these are secrets.
/// </summary>
public static class AuthDefaults
{
    public const string Issuer = "parley-api";

    public const string Audience = "parley-app";

    public const int MinimumSigningKeyBytes = 32;

    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public const int RefreshReuseGraceSeconds = 30;

    public const int MaximumRefreshReuseGraceSeconds = 300;

    public static readonly TimeSpan EmailConfirmationTokenLifetime = TimeSpan.FromHours(24);

    public static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    public const int PasswordMinimumLength = 8;

    public const int MaxFailedAccessAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public const string ConfirmEmailPath = "/confirm-email";

    public const string ResetPasswordPath = "/reset-password";

    public const string AcceptInvitationPath = "/accept-invitation";

    public const string EmailQueueName = "default";

    public const int MaximumPasswordLength = 256;
}
