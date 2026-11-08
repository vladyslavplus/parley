using System.Text;
using Microsoft.Extensions.Options;
using Parley.SharedKernel.Configuration;

namespace Parley.Identity.Options.Auth;

/// <summary>
/// Startup validation for <see cref="AuthOptions"/>. Refuses a missing or placeholder
/// signing key and public base URL in every environment, and refuses signing keys
/// shorter than the HMAC-SHA256 minimum.
/// </summary>
public sealed class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Auth issuer and audience must not be empty.");
        }

        if (!ConfigurationPlaceholders.IsSet(options.SigningKey))
        {
            failures.Add("Auth signing key is missing or still a placeholder. Set Auth__SigningKey.");
        }
        else if (Encoding.UTF8.GetByteCount(options.SigningKey) < AuthDefaults.MinimumSigningKeyBytes)
        {
            failures.Add("Auth signing key must be at least 32 bytes. Set Auth__SigningKey.");
        }

        if (options.AccessTokenLifetime <= TimeSpan.Zero || options.AccessTokenLifetime > TimeSpan.FromHours(1))
        {
            failures.Add("Auth access token lifetime is out of range.");
        }

        if (options.RefreshTokenLifetime <= options.AccessTokenLifetime)
        {
            failures.Add("Auth refresh token lifetime must be longer than the access token lifetime.");
        }

        if (options.RefreshReuseGraceSeconds is < 0 or > AuthDefaults.MaximumRefreshReuseGraceSeconds)
        {
            failures.Add("Auth refresh reuse grace is out of range.");
        }

        if (options.EmailConfirmationTokenLifetime <= TimeSpan.Zero || options.PasswordResetTokenLifetime <= TimeSpan.Zero)
        {
            failures.Add("Auth token lifetimes must be positive.");
        }

        if (options.PasswordMinimumLength is < 6 or > AuthDefaults.MaximumPasswordLength)
        {
            failures.Add("Auth password minimum length is out of range.");
        }

        if (options.MaxFailedAccessAttempts < 1 || options.LockoutDuration <= TimeSpan.Zero)
        {
            failures.Add("Auth lockout settings are out of range.");
        }

        if (!ConfigurationPlaceholders.IsSet(options.PublicBaseUrl)
            || !Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out _))
        {
            failures.Add("Auth public base URL must be an absolute URI and must not be a placeholder. Set Auth__PublicBaseUrl.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
