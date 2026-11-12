using AwesomeAssertions;
using Parley.Notifications.Options;
using Xunit;

namespace Parley.UnitTests.Notifications.Telegram;

public sealed class TelegramOptionRulesTests
{
    private const string Token = "123456:test-token";

    [Fact]
    public void Defaults_AreValid_AndTheChannelIsOffWithoutAToken()
    {
        var options = new TelegramOptions();

        TelegramOptionRules.IsValid(options).Should().BeTrue();
        TelegramOptionRules.IsConfigured(options).Should().BeFalse("an empty token switches the channel off");
    }

    [Fact]
    public void RunningPolling_NeedsAUsername()
    {
        var options = new TelegramOptions { BotToken = Token };

        TelegramOptionRules.IsValid(options).Should().BeFalse();

        options.BotUsername = "parley_test_bot";
        TelegramOptionRules.IsValid(options).Should().BeTrue();
    }

    [Fact]
    public void RunningWebhook_NeedsTheSecretAsWellAsTheUsername()
    {
        var options = new TelegramOptions { BotToken = Token, BotUsername = "parley_test_bot", Mode = TelegramMode.Webhook };

        TelegramOptionRules.IsValid(options).Should().BeFalse("the webhook secret is missing");

        options.WebhookSecret = "a-long-random-webhook-secret";
        TelegramOptionRules.IsValid(options).Should().BeTrue();
    }

    [Fact]
    public void Bounds_RefuseAValueOutsideTheLimits()
    {
        var options = new TelegramOptions { DeliveryMaxAttempts = TelegramOptionLimits.MaximumDeliveryAttempts + 1 };

        TelegramOptionRules.IsValid(options).Should().BeFalse();
    }
}
