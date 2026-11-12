using AwesomeAssertions;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Telegram.Shared;
using Xunit;

namespace Parley.UnitTests.Notifications.Telegram;

/// <summary>The one map from a notification link code to its page in the web app, used by every Telegram text.</summary>
public sealed class NotificationWebPathsTests
{
    [Theory]
    [InlineData(NotificationLinkRoutes.Tasks, "app/tasks")]
    [InlineData(NotificationLinkRoutes.Deals, "app/deals")]
    [InlineData(NotificationLinkRoutes.Campaigns, "app/campaigns")]
    [InlineData(NotificationLinkRoutes.Insights, "app")]
    [InlineData(NotificationLinkRoutes.Team, "app/settings/members")]
    public void ForLinkRoute_PointsAtTheWebRouteOfTheCode(string linkCode, string expectedPath) =>
        NotificationWebPaths.ForLinkRoute(linkCode).Should().Be(expectedPath);

    [Fact]
    public void ForLinkRoute_UnknownCode_FallsBackToTheAppHome()
    {
        NotificationWebPaths.ForLinkRoute("no-such-link").Should().Be(NotificationWebPaths.AppHome);
    }

    [Fact]
    public void BuildUrl_AddsTheCultureAndNeverDoublesTheSlash()
    {
        NotificationWebPaths.BuildUrl("https://app.parley.test/", "uk", NotificationWebPaths.ContactRecord(Guid.Empty))
            .Should().Be("https://app.parley.test/uk/app/contacts/00000000-0000-0000-0000-000000000000");
    }
}
