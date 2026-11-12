namespace Parley.Insights.Options;

/// <summary>
/// Settings of the scoring service integration. Bind to the <c>Insights:Scoring</c> section. The API key is a secret: it comes
/// from configuration or the environment (<c>Insights__Scoring__ApiKey</c>), never from source, and is never logged.
/// </summary>
public sealed class InsightsScoringOptions
{
    public const string SectionName = "Insights:Scoring";

    /// <summary>Largest batch the scoring contract accepts in one request.</summary>
    public const int MaximumBatchSize = 1000;

    /// <summary>Largest clv horizon the scoring contract accepts, in days.</summary>
    public const int MaximumHorizonDays = 3650;

    private const string DefaultQueueName = "default";

    /// <summary>Base address of the scoring service, for example <c>http://127.0.0.1:8090/</c>. Absolute http or https.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Shared key sent in the key header. Empty means the client makes no call: the run fails with a stable code.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Timeout of one attempt against the service, in seconds.</summary>
    public int AttemptTimeoutSeconds { get; set; } = 10;

    /// <summary>Timeout of one call including its retries, in seconds. Never below the attempt timeout.</summary>
    public int TotalTimeoutSeconds { get; set; } = 30;

    /// <summary>Contacts per request to the service, and per read of the CRM metrics.</summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>Largest share of a run's items the service may reject. A run above it fails and swaps nothing in.</summary>
    public decimal MaximumRejectedShare { get; set; } = 0.05m;

    /// <summary>Horizon of the CLV score, in days.</summary>
    public int ClvHorizonDays { get; set; } = 365;

    /// <summary>
    /// A run still queued or running after this long is failed by the sweep as abandoned, so a lost job cannot block its kind.
    /// Set it above the longest healthy run. Environment variable: <c>Insights__Scoring__RunStaleAfter</c>.
    /// </summary>
    public TimeSpan RunStaleAfter { get; set; } = TimeSpan.FromHours(6);

    /// <summary>Hangfire queue the scoring jobs are enqueued on. Must be a queue a server listens on.</summary>
    public string QueueName { get; set; } = DefaultQueueName;
}
