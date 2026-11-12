namespace Parley.Campaigns.Options;

/// <summary>
/// Settings of the send pipeline, the unsubscribe links and the unsubscribe endpoint. Bind to the <c>Campaigns:Send</c>
/// section; validated on start. The token key is a secret: it comes from configuration or the environment, never from
/// code. Changing the attempt limit or the lease never resends a message.
/// </summary>
public sealed class CampaignsSendOptions
{
    public const string SectionName = "Campaigns:Send";

    public const string DefaultQueueName = "default";

    public const int DefaultBatchSize = 50;

    public const int DefaultMaxAttempts = 3;

    public const int DefaultRatePerSecond = 10;

    public const int DefaultUnsubscribePermitLimit = 30;

    public const int MinimumBatchSize = 1;

    public const int MaximumMailAttempts = 10;

    public const int MinimumRatePerSecond = 1;

    public const int MaximumRatePerSecond = 1000;

    public const int MaximumUnsubscribePermitLimit = 10000;

    public const int MinimumUnsubscribeTokenKeyLength = 32;

    public const int MaximumMessageIdDomainLength = 253;

    /// <summary>
    /// Default lease of a claim. It covers a full batch at the default SMTP timeout (`50 * (30 s + 0.1 s)` ≈ 25 min).
    /// </summary>
    public static readonly TimeSpan DefaultLeaseDuration = TimeSpan.FromMinutes(30);

    public static readonly TimeSpan MinimumLeaseDuration = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan MaximumLeaseDuration = TimeSpan.FromHours(1);

    public static readonly TimeSpan DefaultUnsubscribeTokenLifetime = TimeSpan.FromDays(365);

    public static readonly TimeSpan MaximumUnsubscribeTokenLifetime = TimeSpan.FromDays(3650);

    public static readonly TimeSpan DefaultUnsubscribeWindow = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan MaximumUnsubscribeWindow = TimeSpan.FromDays(1);

    public const int DefaultTestSendPermitLimit = 10;

    public const int MaximumTestSendPermitLimit = 1000;

    public static readonly TimeSpan DefaultTestSendWindow = TimeSpan.FromHours(1);

    public static readonly TimeSpan MaximumTestSendWindow = TimeSpan.FromDays(1);

    public static readonly TimeSpan DefaultRetryBackoff = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan MaximumRetryBackoff = TimeSpan.FromHours(1);

    public static readonly TimeSpan DefaultRecoveryInterval = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan MinimumRecoveryInterval = TimeSpan.FromMinutes(1);

    public static readonly TimeSpan MaximumRecoveryInterval = TimeSpan.FromHours(1);

    /// <summary>Hangfire queue send jobs are enqueued on. Must be a queue a server listens on.</summary>
    public string QueueName { get; set; } = DefaultQueueName;

    /// <summary>Recipients claimed per batch. Bounded by the CRM batch maximum, which the recipient reads share.</summary>
    public int BatchSize { get; set; } = DefaultBatchSize;

    /// <summary>Attempts a connection-level failure is retried before the recipient is marked failed.</summary>
    public int MaxAttempts { get; set; } = DefaultMaxAttempts;

    /// <summary>Wait after a failed attempt before its retry is claimed. Zero retries at once.</summary>
    public TimeSpan RetryBackoff { get; set; } = DefaultRetryBackoff;

    /// <summary>
    /// How often the recovery sweep looks for sending campaigns that need a runner: whole minutes, one to sixty. The sweep
    /// is a recurring Hangfire job, so the interval becomes a cron expression when the host starts.
    /// </summary>
    public TimeSpan RecoveryInterval { get; set; } = DefaultRecoveryInterval;

    /// <summary>
    /// Most emails each process starts per second, across all of its send jobs. The limit is held in memory, so it is
    /// per process: N processes that send together can start up to N times this rate. Lower it when you run more than one.
    /// </summary>
    public int RatePerSecond { get; set; } = DefaultRatePerSecond;

    /// <summary>How long a claim is held. A claim whose lease ran out has an unknown outcome and is never resent.</summary>
    public TimeSpan LeaseDuration { get; set; } = DefaultLeaseDuration;

    /// <summary>Domain of the deterministic message id: <c>{campaign_id}.{contact_id}@{domain}</c>.</summary>
    public string MessageIdDomain { get; set; } = string.Empty;

    /// <summary>HMAC key that signs unsubscribe tokens. Secret; at least 32 characters.</summary>
    public string UnsubscribeTokenKey { get; set; } = string.Empty;

    /// <summary>How long an unsubscribe link stays valid after the email is sent.</summary>
    public TimeSpan UnsubscribeTokenLifetime { get; set; } = DefaultUnsubscribeTokenLifetime;

    /// <summary>
    /// Absolute base address of the API. The one-click POST link in the List-Unsubscribe header is built from it, without a
    /// trailing slash.
    /// </summary>
    public string PublicApiBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Absolute base address of the web app, without a trailing slash. The unsubscribe link in the body is
    /// <c>{PublicWebBaseUrl}/{culture}/unsubscribe?token=…</c>, a page that confirms and then calls the API. Set it to the
    /// same address as <c>Auth:PublicBaseUrl</c>.
    /// </summary>
    public string PublicWebBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Unsubscribe requests allowed per client address and window. The window is held in memory, so the limit is per API
    /// process: N API instances allow up to N times this count.
    /// </summary>
    public int UnsubscribePermitLimit { get; set; } = DefaultUnsubscribePermitLimit;

    /// <summary>Length of the unsubscribe rate-limit window.</summary>
    public TimeSpan UnsubscribeWindow { get; set; } = DefaultUnsubscribeWindow;

    /// <summary>
    /// Test sends allowed per signed-in user and window. Each test send is a real mail to the sender's own address, so the
    /// count is kept low. The window is held in memory, so the limit is per API process.
    /// </summary>
    public int TestSendPermitLimit { get; set; } = DefaultTestSendPermitLimit;

    /// <summary>Length of the test-send rate-limit window.</summary>
    public TimeSpan TestSendWindow { get; set; } = DefaultTestSendWindow;
}
