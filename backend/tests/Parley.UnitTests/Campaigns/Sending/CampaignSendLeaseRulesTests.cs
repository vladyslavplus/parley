using AwesomeAssertions;
using Parley.Campaigns.Options;
using Xunit;

namespace Parley.UnitTests.Campaigns.Sending;

/// <summary>The start-up rule that keeps a whole batch inside one claim lease, including the SMTP timeout of every message.</summary>
public sealed class CampaignSendLeaseRulesTests
{
    private static readonly TimeSpan MailTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public void BatchFitsLease_TheDefaultsFitTheDefaultLease()
    {
        var options = new CampaignsSendOptions();

        CampaignSendLeaseRules.BatchFitsLease(options, MailTimeout).Should().BeTrue();
    }

    [Fact]
    public void BatchFitsLease_TheOldShortLease_DoesNotFitAFullBatch()
    {
        var options = new CampaignsSendOptions { LeaseDuration = TimeSpan.FromMinutes(5) };

        CampaignSendLeaseRules.BatchFitsLease(options, MailTimeout).Should().BeFalse();
    }

    [Fact]
    public void BatchFitsLease_ARateOfZero_IsRefused()
    {
        var options = new CampaignsSendOptions { RatePerSecond = 0 };

        CampaignSendLeaseRules.BatchFitsLease(options, MailTimeout).Should().BeFalse();
    }
}
