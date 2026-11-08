namespace Parley.Identity.Options.Auth;

/// <summary>
/// Authentication settings. Bind to the <c>Auth</c> section; validated on start.
/// Environment data (the signing key and the public base URL) has no code default:
/// it comes from configuration, and startup validation refuses a missing value or a
/// placeholder left in committed settings.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = AuthDefaults.Issuer;

    public string Audience { get; set; } = AuthDefaults.Audience;

    /// <summary>HMAC-SHA256 signing key for access tokens. Secret outside Development.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; } = AuthDefaults.AccessTokenLifetime;

    public TimeSpan RefreshTokenLifetime { get; set; } = AuthDefaults.RefreshTokenLifetime;

    /// <summary>
    /// Window in which a just-rotated refresh token still returns a new token
    /// pair. Covers two browser tabs refreshing at the same moment.
    /// </summary>
    public int RefreshReuseGraceSeconds { get; set; } = AuthDefaults.RefreshReuseGraceSeconds;

    public TimeSpan EmailConfirmationTokenLifetime { get; set; } = AuthDefaults.EmailConfirmationTokenLifetime;

    public TimeSpan PasswordResetTokenLifetime { get; set; } = AuthDefaults.PasswordResetTokenLifetime;

    public int PasswordMinimumLength { get; set; } = AuthDefaults.PasswordMinimumLength;

    public int MaxFailedAccessAttempts { get; set; } = AuthDefaults.MaxFailedAccessAttempts;

    public TimeSpan LockoutDuration { get; set; } = AuthDefaults.LockoutDuration;

    /// <summary>Absolute origin of the web app that hosts the confirmation and reset pages.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public string ConfirmEmailPath { get; set; } = AuthDefaults.ConfirmEmailPath;

    public string ResetPasswordPath { get; set; } = AuthDefaults.ResetPasswordPath;

    /// <summary>Path of the web page that accepts an organization invitation.</summary>
    public string AcceptInvitationPath { get; set; } = AuthDefaults.AcceptInvitationPath;

    /// <summary>
    /// Hangfire queue that carries account email jobs. Must be a queue the API
    /// host listens to.
    /// </summary>
    public string EmailQueueName { get; set; } = AuthDefaults.EmailQueueName;
}
