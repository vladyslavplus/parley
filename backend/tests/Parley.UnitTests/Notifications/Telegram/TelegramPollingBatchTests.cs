using AwesomeAssertions;
using Parley.Notifications.Telegram.Bot;
using Parley.Notifications.Telegram.Polling;
using Xunit;

namespace Parley.UnitTests.Notifications.Telegram;

public sealed class TelegramPollingBatchTests
{
    private const int MaximumAttempts = 3;

    [Fact]
    public async Task BadUpdate_IsRetried_ThenSkipped_AndTheRestOfTheBatchStillRuns()
    {
        var cursor = new TelegramPollingCursor();
        var handled = new List<long>();
        var failures = new List<long>();
        var updates = new[]
        {
            new TelegramIncomingUpdate(1, 10, "/today", null),
            new TelegramIncomingUpdate(2, 20, "/today", null),
        };
        Func<TelegramIncomingUpdate, CancellationToken, Task> handle = (update, _) =>
        {
            if (update.UpdateId == 1)
            {
                throw new InvalidOperationException("The handler failed on this update.");
            }

            handled.Add(update.UpdateId);
            return Task.CompletedTask;
        };
        Action<long, int, Exception> onFailure = (updateId, _, _) => failures.Add(updateId);

        // The first two polls stop at update 1 and leave the cursor before it, so it is fetched again.
        (await TelegramPollingBatch.HandleAsync(updates, cursor, MaximumAttempts, handle, onFailure, CancellationToken.None))
            .Should().BeFalse();
        (await TelegramPollingBatch.HandleAsync(updates, cursor, MaximumAttempts, handle, onFailure, CancellationToken.None))
            .Should().BeFalse();
        cursor.Offset.Should().BeNull();
        handled.Should().BeEmpty();

        // The third attempt reaches the limit: update 1 is skipped and update 2 is handled in the same batch.
        (await TelegramPollingBatch.HandleAsync(updates, cursor, MaximumAttempts, handle, onFailure, CancellationToken.None))
            .Should().BeTrue();
        cursor.Offset.Should().Be(3);
        handled.Should().Equal(2L);
        failures.Should().Equal(1L, 1L, 1L);
        cursor.Failures.Should().BeEmpty();
    }
}
