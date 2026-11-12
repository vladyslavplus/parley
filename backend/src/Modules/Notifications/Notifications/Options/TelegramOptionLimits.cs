namespace Parley.Notifications.Options;

/// <summary>Bounds of the Telegram options. Validation and documentation read the same values.</summary>
public static class TelegramOptionLimits
{
    public const int MinimumLinkCodeTtlMinutes = 1;

    public const int MaximumLinkCodeTtlMinutes = 60;

    public const int MaximumLinkCodeRequests = 100;

    public const int MaximumDeliveryAttempts = 10;

    public const int MaximumDeliveryBatchSize = 200;

    public const int MaximumMessagesPerSecond = 30;

    public const int MaximumChatIntervalMilliseconds = 60_000;

    public const int MaximumTodayItems = 20;

    public const int MaximumRetentionDays = 3650;

    public const int MaximumPollingTimeoutSeconds = 50;

    public const int MinimumLeaseSeconds = 10;

    public const int MaximumUpdateAttempts = 10;

    public const int MaximumWebhookPermitLimit = 10_000;
}
