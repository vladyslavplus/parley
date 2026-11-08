namespace Parley.Api.Hangfire;

/// <summary>
/// Background worker settings. Bind to the <c>Hangfire</c> section;
/// validated on start in the host. Overridable by an environment variable
/// such as <c>Hangfire__WorkerCount</c>.
/// </summary>
public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    public const int MinimumWorkerCount = 1;

    public const int MaximumWorkerCount = 100;

    private const int DefaultWorkerCount = 4;

    public const int MinimumRetryAttempts = 0;

    public const int MaximumRetryAttempts = 100;

    private const int DefaultRetryAttempts = 5;

    public const int MinimumDeleteExpiredBatchSize = 1;

    public const int MaximumDeleteExpiredBatchSize = 100000;

    private const int DefaultDeleteExpiredBatchSize = 1000;

    private static readonly TimeSpan DefaultQueuePollInterval = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan DefaultJobExpirationCheckInterval = TimeSpan.FromHours(1);

    private static readonly TimeSpan DefaultJobExpiration = TimeSpan.FromDays(1);

    public static readonly TimeSpan DefaultSchemaInitRetryDelay = TimeSpan.FromSeconds(2);

    public const string DefaultQueueName = "default";

    /// <summary>Maximum number of worker threads picking up jobs.</summary>
    public int WorkerCount { get; set; } = DefaultWorkerCount;

    /// <summary>Queues this server listens on. Defaults to <see cref="DefaultQueueName"/> when not configured.</summary>
    public string[] Queues { get; set; } = [];

    /// <summary>How long completed jobs are kept before the expiration manager deletes them.</summary>
    public TimeSpan JobExpiration { get; set; } = DefaultJobExpiration;

    /// <summary>How often the expiration manager sweeps finished jobs.</summary>
    public TimeSpan JobExpirationCheckInterval { get; set; } = DefaultJobExpirationCheckInterval;

    /// <summary>How many expired jobs one expiration pass deletes at most.</summary>
    public int DeleteExpiredBatchSize { get; set; } = DefaultDeleteExpiredBatchSize;

    /// <summary>Attempts a failing job gets before it is deleted.</summary>
    public int RetryAttempts { get; set; } = DefaultRetryAttempts;

    /// <summary>How often the server fetches new jobs from the queues.</summary>
    public TimeSpan QueuePollInterval { get; set; } = DefaultQueuePollInterval;
}
