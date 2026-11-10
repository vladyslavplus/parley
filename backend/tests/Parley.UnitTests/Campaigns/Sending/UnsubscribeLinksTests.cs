using AwesomeAssertions;
using Parley.Campaigns.Sending.Unsubscribe;
using Xunit;

namespace Parley.UnitTests.Campaigns.Sending;

public sealed class UnsubscribeLinksTests
{
    [Fact]
    public void BuildPage_PutsTheCultureInThePath_AndTheEscapedTokenInTheQuery()
    {
        var link = UnsubscribeLinks.BuildPage("https://app.parley.test/", "uk", "a+b/c");

        link.Should().Be("https://app.parley.test/uk/unsubscribe?token=a%2Bb%2Fc");
    }

    [Fact]
    public void BuildPage_FallsBackToTheDefaultCulture_ForAnUnknownOne()
    {
        var link = UnsubscribeLinks.BuildPage("https://app.parley.test", "fr-FR", "token");

        link.Should().Be("https://app.parley.test/en/unsubscribe?token=token");
    }

    [Fact]
    public void BuildOneClick_PointsAtTheApiEndpoint()
    {
        var link = UnsubscribeLinks.BuildOneClick("https://api.parley.test/", "token");

        link.Should().Be("https://api.parley.test/unsubscribe?token=token");
    }
}
