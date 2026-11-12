using AwesomeAssertions;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Domain.Telegram;
using Xunit;

namespace Parley.UnitTests.Notifications.Telegram;

public sealed class TelegramDeliveryRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    private static readonly TimeSpan Base = TimeSpan.FromSeconds(30);

    [Theory]
    [InlineData(null, TelegramSendOutcome.Unavailable)]
    [InlineData(403, TelegramSendOutcome.Blocked)]
    [InlineData(429, TelegramSendOutcome.RateLimited)]
    [InlineData(400, TelegramSendOutcome.Rejected)]
    [InlineData(401, TelegramSendOutcome.Unavailable)]
    [InlineData(502, TelegramSendOutcome.Unavailable)]
    public void Classify_MapsTheHttpStatusToAnOutcome(int? statusCode, TelegramSendOutcome outcome) =>
        TelegramDeliveryRules.Classify(statusCode).Should().Be(outcome);

    [Fact]
    public void ShouldRetry_OnlyForTransientOutcomes_AndOnlyWhileAttemptsRemain()
    {
        TelegramDeliveryRules.ShouldRetry(TelegramSendOutcome.RateLimited, attemptsSoFar: 1, maximumAttempts: 3).Should().BeTrue();
        TelegramDeliveryRules.ShouldRetry(TelegramSendOutcome.Unavailable, attemptsSoFar: 3, maximumAttempts: 3).Should().BeFalse("the limit is reached");
        TelegramDeliveryRules.ShouldRetry(TelegramSendOutcome.Blocked, attemptsSoFar: 1, maximumAttempts: 3).Should().BeFalse("a blocked chat never retries");
        TelegramDeliveryRules.ShouldRetry(TelegramSendOutcome.Rejected, attemptsSoFar: 1, maximumAttempts: 3).Should().BeFalse("a malformed message never retries");
    }

    [Fact]
    public void NextAttemptAt_HonorsTelegramRetryAfter_BeforeBackoff()
    {
        var next = TelegramDeliveryRules.NextAttemptAt(Now, attemptsSoFar: 1, Base, retryAfter: TimeSpan.FromSeconds(7));

        next.Should().Be(Now.AddSeconds(7));
    }

    [Fact]
    public void NextAttemptAt_DoublesEachAttempt_AndStopsAtTheHourCap()
    {
        TelegramDeliveryRules.NextAttemptAt(Now, attemptsSoFar: 1, Base, retryAfter: null).Should().Be(Now.AddSeconds(30));
        TelegramDeliveryRules.NextAttemptAt(Now, attemptsSoFar: 2, Base, retryAfter: null).Should().Be(Now.AddSeconds(60));
        TelegramDeliveryRules.NextAttemptAt(Now, attemptsSoFar: 3, Base, retryAfter: null).Should().Be(Now.AddSeconds(120));
        TelegramDeliveryRules.NextAttemptAt(Now, attemptsSoFar: 40, Base, retryAfter: null).Should().Be(Now + TelegramDeliveryRules.MaximumBackoff);
    }

    [Theory]
    [InlineData(TelegramSendOutcome.Sent, "sent", null)]
    [InlineData(TelegramSendOutcome.Blocked, "blocked", TelegramErrorCodes.DeliveryBlocked)]
    [InlineData(TelegramSendOutcome.Rejected, "failed", TelegramErrorCodes.DeliveryRejected)]
    [InlineData(TelegramSendOutcome.Unavailable, "failed", TelegramErrorCodes.DeliveryUnavailable)]
    public void StatusAndErrorCode_OfAFinishedAttempt(TelegramSendOutcome outcome, string status, string? errorCode)
    {
        TelegramDeliveryRules.StatusOf(outcome).Should().Be(status);
        TelegramDeliveryRules.ErrorCodeOf(outcome).Should().Be(errorCode);
    }
}
