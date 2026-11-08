using Microsoft.Extensions.Options;

namespace Parley.Identity.Options.RateLimiting;

/// <summary>Startup validation for <see cref="RateLimitingOptions"/>.</summary>
public sealed class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>
{
    private static readonly TimeSpan MinimumWindow = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan MaximumWindow = TimeSpan.FromDays(1);

    public ValidateOptionsResult Validate(string? name, RateLimitingOptions options)
    {
        var policies = new[]
        {
            options.Register,
            options.Login,
            options.Refresh,
            options.ConfirmEmail,
            options.ResendConfirmation,
            options.ForgotPassword,
            options.ResetPassword,
            options.InvitationCreate,
            options.InvitationAccept,
        };

        var valid = policies.All(policy =>
            policy.PermitLimit is >= RateLimitingOptions.MinimumPermitLimit and <= RateLimitingOptions.MaximumPermitLimit
            && policy.Window >= MinimumWindow
            && policy.Window <= MaximumWindow);

        return valid
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Rate limit permits or windows are out of range.");
    }
}
