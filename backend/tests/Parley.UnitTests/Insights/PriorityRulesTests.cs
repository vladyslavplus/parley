using AwesomeAssertions;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Domain.Rfm;
using Xunit;

namespace Parley.UnitTests.Insights;

/// <summary>The priority value, the reason codes and the next action of one contact.</summary>
public sealed class PriorityRulesTests
{
    private const decimal TaskDueBonus = 15m;

    private const decimal StaleDealBonus = 10m;

    private static readonly IReadOnlyDictionary<string, decimal> Weights = new Dictionary<string, decimal> { ["at_risk"] = 90m };

    [Fact]
    public void Score_AddsTheSegmentWeightAndBothBonuses()
    {
        var score = PriorityRules.Score("at_risk", new RfmTiers(1, 3, 1), new PriorityFacts(true, true), Weights, TaskDueBonus, StaleDealBonus);

        score.Value.Should().Be(115m);
    }

    [Fact]
    public void Score_ListsTheReasonsInTheDocumentedOrder()
    {
        var score = PriorityRules.Score("at_risk", new RfmTiers(5, 1, 5), new PriorityFacts(true, true), Weights, TaskDueBonus, StaleDealBonus);

        score.ReasonCodes.Should().Equal(
            PriorityReasons.AtRisk,
            PriorityReasons.TaskDue,
            PriorityReasons.DealStale,
            PriorityReasons.HighValue,
            PriorityReasons.RecentOrder);
    }

    [Fact]
    public void Score_WithoutWeightOrFacts_IsZero()
    {
        var score = PriorityRules.Score("regular", null, new PriorityFacts(false, false), Weights, TaskDueBonus, StaleDealBonus);

        score.Value.Should().Be(0m);
        score.ReasonCodes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("at_risk", false, false, PrioritySuggestedActions.WinBack)]
    [InlineData("champions", false, false, PrioritySuggestedActions.Reward)]
    [InlineData("regular", false, false, PrioritySuggestedActions.Nurture)]
    [InlineData("regular", false, true, PrioritySuggestedActions.Call)]
    [InlineData("champions", true, false, PrioritySuggestedActions.ReviewDeal)]
    [InlineData("regular", true, true, PrioritySuggestedActions.ReviewDeal)]
    public void ActionFor_ReviewsAStaleDealFirst_ThenCallsOnADueTask(string segment, bool staleDeal, bool dueTask, string expected)
    {
        PriorityRules.ActionFor(segment, new PriorityFacts(dueTask, staleDeal)).Should().Be(expected);
    }
}
