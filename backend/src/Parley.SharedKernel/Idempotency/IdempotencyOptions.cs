namespace Parley.SharedKernel.Idempotency;

/// <summary>
/// Settings for <c>Idempotency-Key</c> support. Bind to the <c>Idempotency</c>
/// section; validated on start in the host.
/// </summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    public const long MinimumMaxBodyBytes = 1;

    public const long MaximumAllowedBodyBytes = 16 * 1024 * 1024;

    public const long DefaultMaxBodyBytes = 1024 * 1024;

    public const long MinimumPollIntervalMs = 1;

    public const long MaximumPollIntervalMs = 10000;

    private const long DefaultPollIntervalMs = 50;

    public const long MinimumWaitTimeoutMs = 100;

    public const long MaximumWaitTimeoutMs = 300000;

    private const long DefaultWaitTimeoutMs = 5000;

    public static readonly TimeSpan ZeroLifetime = TimeSpan.Zero;

    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromDays(90);

    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(1);

    /// <summary>Maximum accepted <c>Idempotency-Key</c> length. Longer keys are rejected before they can break the unique index.</summary>
    public const int MaximumKeyLength = 255;

    /// <summary>How long a claimed key stays valid before it may be reused.</summary>
    public TimeSpan KeyLifetime { get; set; } = DefaultLifetime;

    /// <summary>
    /// Module schemas swept by the recurring cleanup job. Empty while modules
    /// are not registered: the job is then a deliberate no-op.
    /// </summary>
    public string[] CleanupSchemas { get; set; } = [];

    /// <summary>Request bodies larger than this are rejected with a validation error.</summary>
    public long MaxRequestBodyBytes { get; set; } = DefaultMaxBodyBytes;

    /// <summary>How often a concurrent caller polls for the in-flight response.</summary>
    public TimeSpan InProgressPollInterval { get; set; } = TimeSpan.FromMilliseconds(DefaultPollIntervalMs);

    /// <summary>How long a concurrent caller waits before returning a conflict.</summary>
    public TimeSpan InProgressWaitTimeout { get; set; } = TimeSpan.FromMilliseconds(DefaultWaitTimeoutMs);
}
