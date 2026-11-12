using Parley.Insights.Contracts.Priority;

namespace Parley.IntegrationTests.Notifications.Telegram.Support;

/// <summary>Priority entries for the tests: one contact owned by the given user, or by nobody, with the given next action.</summary>
public static class TestEntries
{
    public static PriorityEntry Action(Guid? ownerUserId, string suggestedAction) => new(
        ContactId: Guid.NewGuid(),
        OwnerUserId: ownerUserId,
        Rank: 1,
        Value: 1m,
        Mode: "rfm",
        Segment: "loyal",
        ReasonCodes: [],
        SuggestedAction: suggestedAction);
}
