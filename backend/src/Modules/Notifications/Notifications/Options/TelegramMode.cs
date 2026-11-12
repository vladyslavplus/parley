namespace Parley.Notifications.Options;

/// <summary>How the bot receives updates. Both modes feed the same update handler.</summary>
public enum TelegramMode
{
    /// <summary>Long polling from a hosted service. Works on a local machine with no public address.</summary>
    Polling = 1,

    /// <summary>Telegram posts updates to the webhook endpoint, which checks the secret token header.</summary>
    Webhook = 2,
}
