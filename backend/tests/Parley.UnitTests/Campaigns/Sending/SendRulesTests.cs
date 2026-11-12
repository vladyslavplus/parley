using AwesomeAssertions;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Sending.Messages;
using Xunit;

namespace Parley.UnitTests.Campaigns.Sending;

/// <summary>The pure rules that keep a recipient from being sent twice, and the header safety of the subject.</summary>
public sealed class SendRulesTests
{
    private const int MaxAttempts = 3;

    private static readonly DateTime Now = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Lease_EndedAtOrBeforeNow_IsExpired_AndAnOpenLeaseIsNot()
    {
        SendPolicy.IsLeaseExpired(Now, Now).Should().BeTrue("a lease that ends now has ended");
        SendPolicy.IsLeaseExpired(Now.AddSeconds(-1), Now).Should().BeTrue();
        SendPolicy.IsLeaseExpired(Now.AddSeconds(1), Now).Should().BeFalse();
        SendPolicy.IsLeaseExpired(null, Now).Should().BeFalse("a claim without a lease has an outcome");
    }

    [Fact]
    public void NotAccepted_RetriesWhileAttemptsRemain_AndFailsOnTheLastAttempt()
    {
        SendPolicy.AfterNotAccepted(attemptCount: 1, MaxAttempts).Should().Be(SendStatuses.RetryableFailed);
        SendPolicy.AfterNotAccepted(attemptCount: MaxAttempts - 1, MaxAttempts).Should().Be(SendStatuses.RetryableFailed);
        SendPolicy.AfterNotAccepted(attemptCount: MaxAttempts, MaxAttempts).Should().Be(SendStatuses.Failed);
        SendPolicy.CanRetry(MaxAttempts, MaxAttempts).Should().BeFalse();
    }

    [Fact]
    public void RecipientMessageId_IsDeterministic_AndCarriesTheDomain()
    {
        var campaign = Guid.Parse("0192f5a0-7b1c-7000-8000-0000000000b1");
        var contact = Guid.Parse("0192f5a0-7b1c-7000-8000-0000000000b2");

        CampaignMessageIds.ForRecipient(campaign, contact, "parley.test")
            .Should().Be("0192f5a0-7b1c-7000-8000-0000000000b1.0192f5a0-7b1c-7000-8000-0000000000b2@parley.test");
    }

    [Fact]
    public void Subject_LosesItsLineBreaks_SoNoHeaderLineCanBeInjected()
    {
        var sanitized = SubjectHeaderSanitizer.Sanitize("Hello\r\nBcc: someone@example.test\nagain");

        sanitized.Should().Be("Hello  Bcc: someone@example.test again");
        sanitized.Should().NotContain("\r");
        sanitized.Should().NotContain("\n");
    }
}
