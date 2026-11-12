using Parley.Notifications.Telegram.Bot;

namespace Parley.Notifications.Telegram.Polling;

/// <summary>
/// Handles one polled batch in order. An update that fails stops the batch before it, and the next poll fetches it again. After
/// the attempt limit the update is skipped, so one bad update cannot hold back every later update of every user.
/// </summary>
public static class TelegramPollingBatch
{
    /// <summary>
    /// Returns true when the whole batch is handled. Returns false when an update failed and is still below the attempt limit;
    /// the cursor then stays before that update.
    /// </summary>
    public static async Task<bool> HandleAsync(
        IEnumerable<TelegramIncomingUpdate> updates,
        TelegramPollingCursor cursor,
        int maximumAttempts,
        Func<TelegramIncomingUpdate, CancellationToken, Task> handle,
        Action<long, int, Exception> onFailure,
        CancellationToken cancellationToken)
    {
        foreach (var update in updates)
        {
            try
            {
                await handle(update, cancellationToken);
                cursor.Failures.Remove(update.UpdateId);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                var attempts = cursor.Failures.GetValueOrDefault(update.UpdateId) + 1;
                onFailure(update.UpdateId, attempts, exception);
                if (attempts < maximumAttempts)
                {
                    cursor.Failures[update.UpdateId] = attempts;
                    return false;
                }

                // The limit is reached: the update is given up and the cursor moves past it.
                cursor.Failures.Remove(update.UpdateId);
            }

            cursor.Offset = update.UpdateId + 1;
        }

        return true;
    }
}
