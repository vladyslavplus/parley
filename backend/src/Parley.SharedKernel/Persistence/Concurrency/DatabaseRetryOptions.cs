namespace Parley.SharedKernel.Persistence.Concurrency;

/// <summary>
/// Bounded retry settings for PostgreSQL transient failures. Bind to the
/// <c>Database:Retry</c> section; validated on start.
/// </summary>
public sealed class DatabaseRetryOptions
{
    public const string SectionName = "Database:Retry";

    public const int MinimumAttempts = 1;

    public const int MaximumAttempts = 10;

    private const int DefaultAttempts = 3;

    private static readonly TimeSpan DefaultMaxRetryDelay = TimeSpan.FromMilliseconds(500);

    public static readonly TimeSpan MaximumAllowedRetryDelay = TimeSpan.FromSeconds(5);

    public int MaxAttempts { get; set; } = DefaultAttempts;

    public TimeSpan MaxRetryDelay { get; set; } = DefaultMaxRetryDelay;
}
