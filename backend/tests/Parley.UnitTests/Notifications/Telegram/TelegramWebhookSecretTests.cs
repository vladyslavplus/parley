using AwesomeAssertions;
using Parley.Notifications.Telegram.Webhook;
using Xunit;

namespace Parley.UnitTests.Notifications.Telegram;

public sealed class TelegramWebhookSecretTests
{
    [Fact]
    public void Matches_AcceptsTheExactSecret_AndRefusesEveryOtherValue()
    {
        const string secret = "a-long-random-webhook-secret";

        TelegramWebhookSecret.Matches(secret, secret).Should().BeTrue();
        TelegramWebhookSecret.Matches(secret + "x", secret).Should().BeFalse();
        TelegramWebhookSecret.Matches("a-long-random-webhook-secreT", secret).Should().BeFalse();
        TelegramWebhookSecret.Matches(null, secret).Should().BeFalse("a missing header never matches");
        TelegramWebhookSecret.Matches(string.Empty, secret).Should().BeFalse();
    }

    [Fact]
    public void Matches_NeverAcceptsAnEmptyConfiguredSecret()
    {
        TelegramWebhookSecret.Matches(string.Empty, string.Empty).Should().BeFalse();
        TelegramWebhookSecret.Matches("anything", string.Empty).Should().BeFalse();
    }
}
