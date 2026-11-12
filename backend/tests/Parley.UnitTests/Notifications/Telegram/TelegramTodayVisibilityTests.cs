using AwesomeAssertions;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Contracts.Priority;
using Parley.Notifications.Telegram.Today;
using Xunit;

namespace Parley.UnitTests.Notifications.Telegram;

public sealed class TelegramTodayVisibilityTests
{
    private static readonly Guid Me = Guid.Parse("0192f5a0-0000-7000-8000-00000000000a");

    private static readonly Guid Teammate = Guid.Parse("0192f5a0-0000-7000-8000-00000000000b");

    private static readonly Guid Stranger = Guid.Parse("0192f5a0-0000-7000-8000-00000000000c");

    private static readonly IReadOnlyList<PriorityEntry> Pool =
    [
        Entry(Me),
        Entry(Stranger),
        Entry(Teammate),
        Entry(null),
    ];

    [Fact]
    public void OwnScope_SeesOnlyTheMembersOwnEntries()
    {
        var visible = TelegramTodayVisibility.SelectVisible(new DataScope(DataScopeLevel.Own, [Me]), Me, Pool, limit: 5);

        visible.Select(entry => entry.OwnerUserId).Should().Equal(Me);
    }

    [Fact]
    public void TeamScope_SeesTheEntriesOfTheTeamOwnersOnly()
    {
        var visible = TelegramTodayVisibility.SelectVisible(new DataScope(DataScopeLevel.Team, [Me, Teammate]), Me, Pool, limit: 5);

        visible.Select(entry => entry.OwnerUserId).Should().Equal(Me, Teammate);
    }

    [Fact]
    public void OrganizationScope_SeesAllEntries_UpToTheLimit()
    {
        var visible = TelegramTodayVisibility.SelectVisible(new DataScope(DataScopeLevel.Organization, null), Me, Pool, limit: 2);

        visible.Should().HaveCount(2);
    }

    [Fact]
    public void NoScope_SeesNothing()
    {
        TelegramTodayVisibility.SelectVisible(DataScope.Nothing, Me, Pool, limit: 5).Should().BeEmpty();
    }

    private static PriorityEntry Entry(Guid? owner) => new(
        ContactId: Guid.NewGuid(),
        OwnerUserId: owner,
        Rank: 1,
        Value: 1m,
        Mode: "rfm",
        Segment: "loyal",
        ReasonCodes: [],
        SuggestedAction: "action.call");
}
