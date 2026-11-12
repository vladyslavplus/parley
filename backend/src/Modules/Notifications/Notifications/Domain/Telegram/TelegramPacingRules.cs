namespace Parley.Notifications.Domain.Telegram;

/// <summary>
/// Pure pacing of sends. A message waits until the chat's previous message is old enough, and until fewer than the allowed
/// number of messages went out in the last second.
/// </summary>
public static class TelegramPacingRules
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The earliest moment a message to a chat may go out. <paramref name="recentSends"/> holds the scheduled send times in
    /// ascending order; only those within one window of the candidate count.
    /// </summary>
    /// <param name="now">The current instant; nothing is scheduled before it.</param>
    /// <param name="lastSendToChat">When the chat's previous message was scheduled; null when it has none.</param>
    /// <param name="recentSends">Scheduled send times over all chats, ascending.</param>
    /// <param name="chatInterval">Least time between two messages to the same chat.</param>
    /// <param name="maximumPerSecond">Largest number of messages in one window over all chats; zero means no limit.</param>
    public static DateTime NextSlot(
        DateTime now,
        DateTime? lastSendToChat,
        IReadOnlyList<DateTime> recentSends,
        TimeSpan chatInterval,
        int maximumPerSecond)
    {
        var slot = now;
        if (lastSendToChat is { } last && last + chatInterval > slot)
        {
            slot = last + chatInterval;
        }

        if (maximumPerSecond <= 0)
        {
            return slot;
        }

        // Each pass moves the slot past the send that blocks it, so the loop ends once the window has room.
        while (true)
        {
            var inWindow = CountInWindow(recentSends, slot);
            if (inWindow < maximumPerSecond)
            {
                return slot;
            }

            var blocking = recentSends[^maximumPerSecond];
            slot = blocking + Window;
        }
    }

    private static int CountInWindow(IReadOnlyList<DateTime> recentSends, DateTime slot)
    {
        var from = slot - Window;
        var count = 0;
        for (var index = recentSends.Count - 1; index >= 0 && recentSends[index] > from; index--)
        {
            count++;
        }

        return count;
    }
}
