namespace Parley.Notifications.Domain.Telegram;

/// <summary>Stored states of a Telegram delivery. The database check constraint lists the same values.</summary>
public static class TelegramDeliveryStatuses
{
    /// <summary>Waiting for an attempt, or for the next attempt after a retryable failure.</summary>
    public const string Pending = "pending";

    /// <summary>Telegram accepted the message.</summary>
    public const string Sent = "sent";

    /// <summary>The chat blocked the bot. The links of the chat are switched off.</summary>
    public const string Blocked = "blocked";

    /// <summary>Telegram refused the message, or the retries ran out.</summary>
    public const string Failed = "failed";

    /// <summary>The link or the preference ended before the message was sent.</summary>
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All = [Pending, Sent, Blocked, Failed, Cancelled];
}
