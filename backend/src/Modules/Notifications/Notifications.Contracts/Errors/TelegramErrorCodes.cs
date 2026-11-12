namespace Parley.Notifications.Contracts.Errors;

/// <summary>
/// Stable error codes of the Telegram channel. Each code has an English and a Ukrainian text in the shared resources.
/// </summary>
public static class TelegramErrorCodes
{
    /// <summary>No bot token is configured, or the channel is switched off: linking is not offered.</summary>
    public const string NotConfigured = "telegram.not_configured";

    /// <summary>The link code is unknown, already used, or expired.</summary>
    public const string LinkCodeInvalid = "telegram.link_code.invalid";

    /// <summary>The caller asked for too many link codes in the window.</summary>
    public const string LinkCodeRateLimited = "telegram.link_code.rate_limited";

    /// <summary>The caller already has an active link in this organization.</summary>
    public const string LinkAlreadyExists = "telegram.link.already_exists";

    /// <summary>The chat is already linked to another user in this organization.</summary>
    public const string ChatAlreadyLinked = "telegram.chat.already_linked";

    /// <summary>The caller has no active link in this organization.</summary>
    public const string LinkNotFound = "telegram.link.not_found";

    /// <summary>Telegram refused the message: the chat blocked the bot. The link is switched off.</summary>
    public const string DeliveryBlocked = "telegram.delivery.blocked";

    /// <summary>Telegram refused the message as malformed.</summary>
    public const string DeliveryRejected = "telegram.delivery.rejected";

    /// <summary>The webhook request does not carry the configured secret token.</summary>
    public const string WebhookSecretInvalid = "telegram.webhook.secret_invalid";

    /// <summary>Telegram was unreachable or failed, and the retries ran out.</summary>
    public const string DeliveryUnavailable = "telegram.delivery.unavailable";

    /// <summary>Telegram asked the bot to wait; the message waits for the next attempt.</summary>
    public const string DeliveryRateLimited = "telegram.delivery.rate_limited";
}
