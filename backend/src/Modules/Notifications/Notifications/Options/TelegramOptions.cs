namespace Parley.Notifications.Options;

/// <summary>
/// Settings of the Telegram channel. Bind to the <c>Telegram</c> section. Secrets come from configuration or the environment
/// only: <c>Telegram__BotToken</c> and <c>Telegram__WebhookSecret</c>. Without a token the channel is off and the app runs
/// normally. Secrets are never logged and never returned in a response.
/// </summary>
public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Switch of the channel. The channel also needs a token.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How updates arrive: polling (default) or webhook.</summary>
    public TelegramMode Mode { get; set; } = TelegramMode.Polling;

    /// <summary>Bot token from BotFather. Environment variable: <c>Telegram__BotToken</c>.</summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>Bot username without the leading at sign, for the deep link. Environment variable: <c>Telegram__BotUsername</c>.</summary>
    public string BotUsername { get; set; } = string.Empty;

    /// <summary>Secret of the webhook header, required in webhook mode. Environment variable: <c>Telegram__WebhookSecret</c>.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Validity of a link code.</summary>
    public TimeSpan LinkCodeTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Link codes one user may request in one window.</summary>
    public int LinkCodeRequestsPerWindow { get; set; } = 5;

    /// <summary>Window of the link code limit.</summary>
    public TimeSpan LinkCodeWindow { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Attempts of one delivery before it is marked failed.</summary>
    public int DeliveryMaxAttempts { get; set; } = 5;

    /// <summary>First retry delay of a delivery; it doubles on each further attempt, up to one hour.</summary>
    public TimeSpan DeliveryBaseBackoff { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How long a claimed delivery stays claimed before another sweep may take it.</summary>
    public TimeSpan DeliveryClaimLease { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Deliveries one sweep claims at once.</summary>
    public int DeliveryBatchSize { get; set; } = 50;

    /// <summary>
    /// Whether this process sends due deliveries and cleans the update table. On by default. Turn it off on a process that only
    /// serves requests, or in a test that sends deliveries itself. Environment variable: <c>Telegram__DeliveryWorkerEnabled</c>.
    /// </summary>
    public bool DeliveryWorkerEnabled { get; set; } = true;

    /// <summary>Least time between two messages to the same chat. Telegram allows about one per second per chat.</summary>
    public int MinimumChatIntervalMilliseconds { get; set; } = 1000;

    /// <summary>Messages per second over all chats. Telegram allows about thirty; zero turns this pacing off.</summary>
    public int MaximumMessagesPerSecond { get; set; } = 25;

    /// <summary>Items in the <c>/today</c> answer.</summary>
    public int TodayItemCount { get; set; } = 5;

    /// <summary>
    /// Failed attempts of one polled update before the poller skips it, so one bad update cannot stall the bot for everyone.
    /// Each failure is logged with the update id and its type only.
    /// </summary>
    public int MaximumUpdateAttempts { get; set; } = 3;

    /// <summary>Webhook requests one client address may send in one window.</summary>
    public int WebhookPermitLimit { get; set; } = 600;

    /// <summary>Window of the webhook request limit.</summary>
    public TimeSpan WebhookWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Days an update id stays in the dedupe table.</summary>
    public int UpdateRetentionDays { get; set; } = 30;

    /// <summary>Long-poll timeout of one update call, in seconds.</summary>
    public int PollingTimeoutSeconds { get; set; } = 25;

    /// <summary>Largest pause between failed polls.</summary>
    public TimeSpan PollingMaximumBackoff { get; set; } = TimeSpan.FromMinutes(1);
}
