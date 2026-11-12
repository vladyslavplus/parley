using Microsoft.Extensions.Options;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Options;

namespace Parley.Notifications.Telegram.Bot;

/// <summary>
/// Spaces the sends: one chat gets its messages at the configured interval, and all chats together stay under the configured
/// rate. The schedule is kept in memory for this process, which is the only sender. Waiting uses the injected clock.
/// </summary>
public sealed class TelegramSendPacer(IOptions<TelegramOptions> options, TimeProvider timeProvider)
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(1);

    private readonly Lock _gate = new();

    private readonly Dictionary<long, DateTime> _lastByChat = [];

    private readonly List<DateTime> _scheduled = [];

    /// <summary>Returns when the next message to <paramref name="chatId"/> may go out.</summary>
    public async Task WaitTurnAsync(long chatId, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var chatInterval = TimeSpan.FromMilliseconds(settings.MinimumChatIntervalMilliseconds);
        DateTime slot;
        lock (_gate)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            Forget(now, chatInterval);
            var last = _lastByChat.TryGetValue(chatId, out var previous) ? (DateTime?)previous : null;
            slot = TelegramPacingRules.NextSlot(now, last, _scheduled, chatInterval, settings.MaximumMessagesPerSecond);
            Schedule(slot);
            _lastByChat[chatId] = slot;
        }

        var delay = slot - timeProvider.GetUtcNow().UtcDateTime;
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, timeProvider, cancellationToken);
        }
    }

    private void Forget(DateTime now, TimeSpan chatInterval)
    {
        _scheduled.RemoveAll(sent => sent <= now - Window);
        foreach (var chatId in _lastByChat.Where(entry => entry.Value + chatInterval <= now).Select(entry => entry.Key).ToArray())
        {
            _lastByChat.Remove(chatId);
        }
    }

    private void Schedule(DateTime slot)
    {
        var index = _scheduled.BinarySearch(slot);
        _scheduled.Insert(index < 0 ? ~index : index, slot);
    }
}
