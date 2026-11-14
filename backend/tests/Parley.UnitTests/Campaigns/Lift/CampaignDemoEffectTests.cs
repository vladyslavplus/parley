using AwesomeAssertions;
using Parley.Campaigns.Domain.DemoSimulation;
using Parley.Campaigns.Features.Campaigns.DemoSeed;
using Xunit;

namespace Parley.UnitTests.Campaigns.Lift;

/// <summary>
/// The effect the injected demo campaign applies: the documented effect of one contact times the campaign multiplier,
/// kept a probability. The A/A campaign applies no effect at all, so it stays a true no-effect control.
/// </summary>
public sealed class CampaignDemoEffectTests
{
    [Theory]
    [InlineData(10, 1, 1, 40)]
    [InlineData(400, 0, 3, 0)]
    [InlineData(5, 8, 9, 2000)]
    [InlineData(90, 2, 2, 120)]
    [InlineData(90, 1, 2, 60)]
    public void TheInjectedCampaign_ScalesTheDocumentedEffect(double recency, double orders365, double orderCount, double value365)
    {
        var features = Features(recency, orders365, orderCount, value365);

        var documented = DemoEffect.Tau(features);
        var injected = CampaignDemoEffect.Tau(features);

        injected.Should().Be(documented * 2, "the campaign applies the documented effect times the campaign multiplier");
        injected.Should().BeGreaterThanOrEqualTo(-1d).And.BeLessThanOrEqualTo(1d, "an effect stays a probability");
    }

    [Fact]
    public void ThePersuadableContact_CarriesTheLargestPositiveEffectOfTheInjectedCampaign()
    {
        var persuadable = CampaignDemoEffect.Tau(Features(90, 2, 2, 120));

        persuadable.Should().BeApproximately(0.60, 1e-12);
        persuadable.Should().BeGreaterThan(CampaignDemoEffect.Tau(Features(400, 0, 3, 0)), "only lost_cause is smaller");
    }

    private static DemoContactFeatures Features(double recency, double orders365, double orderCount, double value365) =>
        new(
            RecencyDays: recency,
            TenureDays: 400d,
            Orders90Days: 0d,
            Orders365Days: orders365,
            OrderCount: orderCount,
            Value365Days: value365,
            AvgOrderValue: orders365 > 0 ? value365 / orders365 : 0d,
            HasOpenDeal: 0d);
}
