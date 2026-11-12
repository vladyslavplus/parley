using Parley.Notifications.Contracts.Errors;

namespace Parley.Notifications.Domain.Telegram;

/// <summary>Pure decisions of a delivery: how an attempt is classified, when to retry, and the stored state.</summary>
public static class TelegramDeliveryRules
{
    /// <summary>Longest pause between two attempts of one delivery.</summary>
    public static readonly TimeSpan MaximumBackoff = TimeSpan.FromHours(1);

    private const int MaximumExponent = 20;

    /// <summary>Maps the HTTP status of a failed call to an outcome. A null status means no answer at all.</summary>
    public static TelegramSendOutcome Classify(int? statusCode) => statusCode switch
    {
        null => TelegramSendOutcome.Unavailable,
        403 => TelegramSendOutcome.Blocked,
        429 => TelegramSendOutcome.RateLimited,
        401 or >= 500 => TelegramSendOutcome.Unavailable,
        _ => TelegramSendOutcome.Rejected,
    };

    /// <summary>A retry is due only for a transient outcome, and only while attempts remain.</summary>
    public static bool ShouldRetry(TelegramSendOutcome outcome, int attemptsSoFar, int maximumAttempts) =>
        (outcome is TelegramSendOutcome.RateLimited or TelegramSendOutcome.Unavailable) && attemptsSoFar < maximumAttempts;

    /// <summary>
    /// The next attempt time. Telegram's retry time wins when it gave one. Otherwise the delay doubles from the base with each
    /// attempt, up to <see cref="MaximumBackoff"/>.
    /// </summary>
    public static DateTime NextAttemptAt(DateTime now, int attemptsSoFar, TimeSpan baseBackoff, TimeSpan? retryAfter)
    {
        if (retryAfter is { } wait && wait > TimeSpan.Zero)
        {
            return now + wait;
        }

        var exponent = Math.Min(Math.Max(0, attemptsSoFar - 1), MaximumExponent);
        var delay = TimeSpan.FromTicks(baseBackoff.Ticks * (1L << exponent));
        return now + (delay > MaximumBackoff ? MaximumBackoff : delay);
    }

    /// <summary>The stored state after an attempt that did not end in a retry.</summary>
    public static string StatusOf(TelegramSendOutcome outcome) => outcome switch
    {
        TelegramSendOutcome.Sent => TelegramDeliveryStatuses.Sent,
        TelegramSendOutcome.Blocked => TelegramDeliveryStatuses.Blocked,
        _ => TelegramDeliveryStatuses.Failed,
    };

    /// <summary>The stable error code stored for an outcome; null for a sent message.</summary>
    public static string? ErrorCodeOf(TelegramSendOutcome outcome) => outcome switch
    {
        TelegramSendOutcome.Blocked => TelegramErrorCodes.DeliveryBlocked,
        TelegramSendOutcome.Rejected => TelegramErrorCodes.DeliveryRejected,
        TelegramSendOutcome.RateLimited => TelegramErrorCodes.DeliveryRateLimited,
        TelegramSendOutcome.Unavailable => TelegramErrorCodes.DeliveryUnavailable,
        _ => null,
    };
}
