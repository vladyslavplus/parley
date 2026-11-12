using Parley.Notifications.Telegram.Bot;

namespace Parley.IntegrationTests.Notifications.Telegram.Support;

/// <summary>
/// The bot client of the tests. It records every sent message and never reaches the network. A failure can be scripted for the next
/// send to one chat, so the delivery rules see the same statuses Telegram would answer with.
/// </summary>
public sealed class FakeTelegramBotClient : ITelegramBotClient
{
    private readonly Lock _gate = new();

    private readonly List<SentMessage> _sent = [];

    private readonly Dictionary<long, Queue<TelegramCallException>> _failures = [];

    public IReadOnlyList<SentMessage> Sent
    {
        get
        {
            lock (_gate)
            {
                return _sent.ToArray();
            }
        }
    }

    public void FailNextSend(long chatId, int statusCode, TimeSpan? retryAfter = null)
    {
        lock (_gate)
        {
            if (!_failures.TryGetValue(chatId, out var queue))
            {
                queue = new Queue<TelegramCallException>();
                _failures[chatId] = queue;
            }

            queue.Enqueue(new TelegramCallException(statusCode, retryAfter));
        }
    }

    public Task<IReadOnlyList<TelegramIncomingUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TelegramIncomingUpdate>>([]);

    public Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_failures.TryGetValue(chatId, out var queue) && queue.Count > 0)
            {
                throw queue.Dequeue();
            }

            _sent.Add(new SentMessage(chatId, text));
        }

        return Task.CompletedTask;
    }
}
