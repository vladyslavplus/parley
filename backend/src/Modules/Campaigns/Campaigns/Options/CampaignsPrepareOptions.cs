namespace Parley.Campaigns.Options;

/// <summary>Settings of the prepare job and its recovery sweep. Bind to the <c>Campaigns:Prepare</c> section.</summary>
public sealed class CampaignsPrepareOptions
{
    public const string SectionName = "Campaigns:Prepare";

    private const string DefaultQueueName = "default";

    private const int DefaultBatchSize = 1000;

    public static readonly TimeSpan DefaultRecoveryInterval = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan DefaultRecoveryAge = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan MinimumRecoveryInterval = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan MaximumRecoveryInterval = TimeSpan.FromHours(1);

    public static readonly TimeSpan MaximumRecoveryAge = TimeSpan.FromDays(1);

    /// <summary>Hangfire queue prepare jobs are enqueued on. Must be a queue a server listens on.</summary>
    public string QueueName { get; set; } = DefaultQueueName;

    /// <summary>Recipients written per insert statement during the freeze.</summary>
    public int BatchSize { get; set; } = DefaultBatchSize;

    /// <summary>
    /// How often the recovery sweep looks for campaigns stuck in <c>preparing</c>: whole minutes, one to sixty. It covers a
    /// lost enqueue and a worker that died mid-freeze.
    /// </summary>
    public TimeSpan RecoveryInterval { get; set; } = DefaultRecoveryInterval;

    /// <summary>
    /// A preparing campaign not written for this long is queued again. A run that is still alive keeps the lock, so the
    /// extra run exits at once.
    /// </summary>
    public TimeSpan RecoveryAge { get; set; } = DefaultRecoveryAge;
}
