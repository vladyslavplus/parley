using Parley.SharedKernel.Configuration;

namespace Parley.Notifications.Options;

/// <summary>
/// Startup checks of the Telegram options. A value the channel cannot run with stops the host before any job starts. The
/// token is only checked for presence: a missing token switches the channel off instead of failing the start.
/// </summary>
public static class TelegramOptionRules
{
    /// <summary>The message the startup check names when the options are refused.</summary>
    public const string InvalidMessage =
        "Telegram options are out of range, or a running bot needs Telegram__BotUsername, and Telegram__WebhookSecret in webhook mode.";

    /// <summary>True when a real token is configured and the channel is switched on.</summary>
    public static bool IsConfigured(TelegramOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Enabled && ConfigurationPlaceholders.IsSet(options.BotToken);
    }

    /// <summary>The numeric bounds and the mode; a running channel also needs its username, and webhook mode its secret.</summary>
    public static bool IsValid(TelegramOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Enum.IsDefined(options.Mode)
            && IsWithin(options.LinkCodeTtl.TotalMinutes, TelegramOptionLimits.MinimumLinkCodeTtlMinutes, TelegramOptionLimits.MaximumLinkCodeTtlMinutes)
            && IsWithin(options.LinkCodeRequestsPerWindow, 1, TelegramOptionLimits.MaximumLinkCodeRequests)
            && options.LinkCodeWindow > TimeSpan.Zero
            && IsWithin(options.DeliveryMaxAttempts, 1, TelegramOptionLimits.MaximumDeliveryAttempts)
            && options.DeliveryBaseBackoff > TimeSpan.Zero
            && options.DeliveryClaimLease >= TimeSpan.FromSeconds(TelegramOptionLimits.MinimumLeaseSeconds)
            && IsWithin(options.DeliveryBatchSize, 1, TelegramOptionLimits.MaximumDeliveryBatchSize)
            && IsWithin(options.MinimumChatIntervalMilliseconds, 0, TelegramOptionLimits.MaximumChatIntervalMilliseconds)
            && options.DeliveryClaimLease >= LeaseNeededForBatch(options)
            && IsWithin(options.MaximumUpdateAttempts, 1, TelegramOptionLimits.MaximumUpdateAttempts)
            && IsWithin(options.WebhookPermitLimit, 1, TelegramOptionLimits.MaximumWebhookPermitLimit)
            && options.WebhookWindow > TimeSpan.Zero
            && IsWithin(options.MaximumMessagesPerSecond, 0, TelegramOptionLimits.MaximumMessagesPerSecond)
            && IsWithin(options.TodayItemCount, 1, TelegramOptionLimits.MaximumTodayItems)
            && IsWithin(options.UpdateRetentionDays, 1, TelegramOptionLimits.MaximumRetentionDays)
            && IsWithin(options.PollingTimeoutSeconds, 1, TelegramOptionLimits.MaximumPollingTimeoutSeconds)
            && options.PollingMaximumBackoff > TimeSpan.Zero
            && (!IsConfigured(options) || RunningChannelIsComplete(options));
    }

    /// <summary>
    /// A claimed batch sends one message per chat at most every interval, so a batch can take batch size times the interval. The
    /// lease must outlast that twice over, or a slow batch is claimed again while it is still sending.
    /// </summary>
    private static TimeSpan LeaseNeededForBatch(TelegramOptions options) =>
        TimeSpan.FromMilliseconds(2d * options.DeliveryBatchSize * options.MinimumChatIntervalMilliseconds);

    private static bool RunningChannelIsComplete(TelegramOptions options) =>
        ConfigurationPlaceholders.IsSet(options.BotUsername)
        && (options.Mode != TelegramMode.Webhook || ConfigurationPlaceholders.IsSet(options.WebhookSecret));

    private static bool IsWithin(int value, int minimum, int maximum) => value >= minimum && value <= maximum;

    private static bool IsWithin(double value, double minimum, double maximum) => value >= minimum && value <= maximum;
}
