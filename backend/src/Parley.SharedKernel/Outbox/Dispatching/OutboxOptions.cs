namespace Parley.SharedKernel.Outbox.Dispatching;

/// <summary>
/// Dispatcher settings. Bind to the <c>Outbox</c> section; validated on start
/// in the host.
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public const int MinimumBatchSize = 1;

    public const int MaximumBatchSize = 1000;

    public const int MinimumMaxAttempts = 1;

    public const int MaximumMaxAttempts = 100;

    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);

    private const int DefaultBatchSize = 50;

    private const int DefaultMaxAttempts = 5;

    private static readonly TimeSpan DefaultBaseBackoff = TimeSpan.FromSeconds(5);

    public TimeSpan PollInterval { get; set; } = DefaultPollInterval;

    public int BatchSize { get; set; } = DefaultBatchSize;

    public int MaxAttempts { get; set; } = DefaultMaxAttempts;

    public TimeSpan BaseBackoff { get; set; } = DefaultBaseBackoff;
}
