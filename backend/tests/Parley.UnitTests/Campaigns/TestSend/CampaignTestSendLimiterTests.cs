using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Features.Campaigns.TestSend;
using Parley.Campaigns.Options;
using Xunit;

namespace Parley.UnitTests.Campaigns.TestSend;

public sealed class CampaignTestSendLimiterTests
{
    [Fact]
    public void TestSends_AreLimitedPerUser_WithinTheWindow()
    {
        using var limiter = new CampaignTestSendLimiter(Options.Create(new CampaignsSendOptions
        {
            TestSendPermitLimit = 2,
            TestSendWindow = TimeSpan.FromHours(1),
        }));
        var user = Guid.CreateVersion7();
        var otherUser = Guid.CreateVersion7();

        limiter.TryAcquire(user).Should().BeTrue();
        limiter.TryAcquire(user).Should().BeTrue();
        limiter.TryAcquire(user).Should().BeFalse("the window's permits are used up");
        limiter.TryAcquire(otherUser).Should().BeTrue("the limit is per user, not shared");
    }
}
