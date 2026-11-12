namespace Parley.Notifications.Telegram.Bot;

/// <summary>
/// A failed call to the Telegram API. It carries only the HTTP status and Telegram's retry time, never the request, the token,
/// or Telegram's text, so nothing secret reaches a log.
/// </summary>
public sealed class TelegramCallException(int? statusCode, TimeSpan? retryAfter)
    : Exception("The Telegram call failed.")
{
    /// <summary>HTTP status Telegram answered with; null when no answer arrived.</summary>
    public int? StatusCode { get; } = statusCode;

    /// <summary>How long Telegram asked to wait; set with status 429.</summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
