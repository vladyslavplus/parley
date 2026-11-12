namespace Parley.Notifications.Telegram.Polling;

/// <summary>Where polling reads next, and how often each update has failed so far in this process.</summary>
public sealed class TelegramPollingCursor
{
    /// <summary>Offset of the next poll; null before the first poll.</summary>
    public long? Offset { get; set; }

    /// <summary>Failed attempts per update id. Kept in memory: a restart starts the count again.</summary>
    public Dictionary<long, int> Failures { get; } = [];
}
