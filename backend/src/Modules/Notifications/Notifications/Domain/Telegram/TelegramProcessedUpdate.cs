namespace Parley.Notifications.Domain.Telegram;

/// <summary>
/// An update of the bot that was handled. Its primary key is the Telegram update id, so a replayed update fails to insert and
/// has no effect. Rows older than the retention period are removed. Not tenant-filtered: updates arrive before a tenant is known.
/// </summary>
public sealed class TelegramProcessedUpdate
{
    public long UpdateId { get; set; }

    public DateTime ReceivedAt { get; set; }
}
