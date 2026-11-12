using System.Security.Cryptography;
using System.Text;

namespace Parley.Notifications.Telegram.Webhook;

/// <summary>The check of the webhook's secret header. Telegram sends the secret in this header on every update.</summary>
public static class TelegramWebhookSecret
{
    /// <summary>Header Telegram sends with the secret token set by <c>setWebhook</c>.</summary>
    public const string HeaderName = "X-Telegram-Bot-Api-Secret-Token";

    /// <summary>
    /// True when the presented secret equals the configured one. The comparison takes the same time whatever bytes differ, so the
    /// secret cannot be guessed one character at a time. An empty secret never matches.
    /// </summary>
    public static bool Matches(string? presented, string configured)
    {
        if (string.IsNullOrEmpty(presented) || string.IsNullOrEmpty(configured))
        {
            return false;
        }

        var presentedBytes = Encoding.UTF8.GetBytes(presented);
        var configuredBytes = Encoding.UTF8.GetBytes(configured);
        return presentedBytes.Length == configuredBytes.Length
            && CryptographicOperations.FixedTimeEquals(presentedBytes, configuredBytes);
    }
}
