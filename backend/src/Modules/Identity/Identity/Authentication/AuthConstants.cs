namespace Parley.Identity.Authentication;

/// <summary>
/// Names used by the authentication flows: token providers, token sizes and the
/// Hangfire job names. Routes live in the API host next to the controller.
/// </summary>
public static class AuthConstants
{
    /// <summary>Token provider for the email confirmation link (24 hours by default).</summary>
    public const string EmailConfirmationTokenProvider = "parley-email-confirmation";

    /// <summary>Token provider for the password reset link (1 hour by default).</summary>
    public const string PasswordResetTokenProvider = "parley-password-reset";

    /// <summary>Random bytes behind each opaque refresh token (256 bits).</summary>
    public const int RefreshTokenByteLength = 32;
}
