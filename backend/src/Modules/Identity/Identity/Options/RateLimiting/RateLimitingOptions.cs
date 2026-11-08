namespace Parley.Identity.Options.RateLimiting;


/// <summary>
/// Per-endpoint rate limits for the authentication endpoints. Bind to the
/// <c>RateLimiting</c> section; validated on start. Each policy is a fixed window
/// partitioned by client address.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public const int MinimumPermitLimit = 1;

    public const int MaximumPermitLimit = 10000;

    public FixedWindowLimitOptions Register { get; set; } = new(10, TimeSpan.FromHours(1));

    public FixedWindowLimitOptions Login { get; set; } = new(10, TimeSpan.FromMinutes(1));

    public FixedWindowLimitOptions Refresh { get; set; } = new(30, TimeSpan.FromMinutes(1));

    public FixedWindowLimitOptions ConfirmEmail { get; set; } = new(20, TimeSpan.FromHours(1));

    public FixedWindowLimitOptions ResendConfirmation { get; set; } = new(5, TimeSpan.FromHours(1));

    public FixedWindowLimitOptions ForgotPassword { get; set; } = new(5, TimeSpan.FromHours(1));

    public FixedWindowLimitOptions ResetPassword { get; set; } = new(10, TimeSpan.FromHours(1));

    public FixedWindowLimitOptions InvitationCreate { get; set; } = new(50, TimeSpan.FromHours(1));

    public FixedWindowLimitOptions InvitationAccept { get; set; } = new(20, TimeSpan.FromHours(1));
}
