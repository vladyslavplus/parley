namespace Parley.Notifications.Domain.Telegram;

/// <summary>What a send attempt meant, as the delivery rules see it.</summary>
public enum TelegramSendOutcome
{
    Sent = 1,

    /// <summary>The chat blocked the bot (HTTP 403).</summary>
    Blocked,

    /// <summary>Telegram refused the request as malformed (other 4xx).</summary>
    Rejected,

    /// <summary>Telegram asked to wait (HTTP 429). The retry time is the one Telegram gave.</summary>
    RateLimited,

    /// <summary>No answer, a server error, or a token problem. Retried until the limit.</summary>
    Unavailable,
}
