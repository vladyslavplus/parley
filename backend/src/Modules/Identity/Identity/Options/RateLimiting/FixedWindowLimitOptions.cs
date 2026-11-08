namespace Parley.Identity.Options.RateLimiting;


/// <summary>A fixed-window permit budget.</summary>
public sealed class FixedWindowLimitOptions(int permitLimit, TimeSpan window)
{
    public FixedWindowLimitOptions() : this(0, TimeSpan.Zero)
    {
    }

    /// <summary>Requests allowed per window.</summary>
    public int PermitLimit { get; set; } = permitLimit;

    /// <summary>Window length.</summary>
    public TimeSpan Window { get; set; } = window;
}
