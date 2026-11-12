using AwesomeAssertions;
using Parley.Notifications.Domain.Telegram;
using Xunit;

namespace Parley.UnitTests.Notifications.Telegram;

public sealed class TelegramPacingRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    private static readonly TimeSpan ChatInterval = TimeSpan.FromSeconds(1);

    [Fact]
    public void NextSlot_WaitsForTheChatInterval_AfterTheChatsLastMessage()
    {
        var slot = TelegramPacingRules.NextSlot(
            Now,
            lastSendToChat: Now.AddMilliseconds(-300),
            recentSends: [],
            ChatInterval,
            maximumPerSecond: 0);

        slot.Should().Be(Now.AddMilliseconds(700));
    }

    [Fact]
    public void NextSlot_IsNow_WhenTheChatIsQuietAndTheRateHasRoom()
    {
        TelegramPacingRules.NextSlot(Now, lastSendToChat: null, recentSends: [], ChatInterval, maximumPerSecond: 2)
            .Should().Be(Now);
    }

    [Fact]
    public void NextSlot_DelaysUntilAnEarlierSendLeavesTheWindow_WhenAllChatsReachTheRate()
    {
        var recent = new List<DateTime> { Now.AddMilliseconds(100), Now.AddMilliseconds(200) };

        var slot = TelegramPacingRules.NextSlot(Now.AddMilliseconds(300), lastSendToChat: null, recent, ChatInterval, maximumPerSecond: 2);

        slot.Should().Be(Now.AddMilliseconds(1100), "the send at 100 ms leaves the one-second window at 1100 ms");
    }
}
