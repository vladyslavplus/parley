using AwesomeAssertions;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Domain.Priority;
using Xunit;

namespace Parley.UnitTests.Insights;

/// <summary>The ML mode of the priority rule: one value scale, the do-not-disturb caution, the reasons and the status.</summary>
public sealed class PriorityMlRulesTests
{
    private static readonly IReadOnlyDictionary<string, decimal> Weights = new Dictionary<string, decimal>
    {
        [RfmSegmentNames.AtRisk] = 90m,
        [RfmSegmentNames.Champions] = 60m,
    };

    private static readonly PriorityMlOptions Options = new(0.7m, 0.3m, 0.10m, Weights, 15m, 10m);

    private static readonly PriorityFacts NoFacts = new(false, false);

    [Fact]
    public void ScoreMl_KeepsADoNotDisturbContactOnTheList_WithTheLowestValueAndACaution()
    {
        var score = PriorityRules.ScoreMl(RfmSegmentNames.AtRisk, null, NoFacts, 0.9m, PriorityUpliftSegments.DoNotDisturb, Options);
        var ranked = PriorityRules.ScoreMl(RfmSegmentNames.AtRisk, null, NoFacts, 0.1m, PriorityUpliftSegments.Persuadable, Options);

        score.Value.Should().Be(0m, "the caution contact ranks below every contact with a positive value");
        score.ReasonCodes.Should().Contain(PriorityReasons.DoNotDisturb);
        score.SuggestedAction.Should().Be(PrioritySuggestedActions.Hold, "a caution is never a call or a win-back");
        ranked.Value.Should().BeGreaterThan(score.Value);
    }

    [Fact]
    public void ScoreMl_ADoNotDisturbContactWithADueTask_StillHolds()
    {
        var score = PriorityRules.ScoreMl(
            RfmSegmentNames.AtRisk,
            null,
            new PriorityFacts(HasDueTask: true, HasStaleDeal: true),
            0.9m,
            PriorityUpliftSegments.DoNotDisturb,
            Options);

        score.SuggestedAction.Should().Be(PrioritySuggestedActions.Hold);
    }

    [Fact]
    public void ScoreMl_RanksAScoredContactAboveAnUnscoredOneOfTheSameSegment()
    {
        // Both champions: 90 is the largest segment weight and 60/90 is the normalized champion weight, so the scored
        // contact is 90 * (0.7 * 0.2 + 0.3 * 60/90) = 30.6 and the unscored one is 90 * (0 + 0.3 * 60/90) = 18.
        var scored = PriorityRules.ScoreMl(RfmSegmentNames.Champions, null, NoFacts, 0.2m, PriorityUpliftSegments.Persuadable, Options);
        var unscored = PriorityRules.ScoreMl(RfmSegmentNames.Champions, null, NoFacts, 0m, string.Empty, Options);

        scored.Value.Should().Be(30.6m);
        unscored.Value.Should().Be(18m);
        scored.Value.Should().BeGreaterThan(unscored.Value);
    }

    [Fact]
    public void ScoreMl_RanksAHighUpliftWithoutATask_AboveALowUpliftWithADueTask()
    {
        // High: 90 * (0.7 * 0.5 + 0.3 * 60/90) = 49.5. Low: 90 * (0.7 * 0.05 + 0.3 * 60/90) + 15 = 36.15. On the old
        // mixed scale the 15-point bonus alone would put the low-uplift contact first.
        var high = PriorityRules.ScoreMl(RfmSegmentNames.Champions, null, NoFacts, 0.5m, PriorityUpliftSegments.Persuadable, Options);
        var low = PriorityRules.ScoreMl(
            RfmSegmentNames.Champions,
            null,
            new PriorityFacts(HasDueTask: true, HasStaleDeal: false),
            0.05m,
            PriorityUpliftSegments.Persuadable,
            Options);

        high.Value.Should().Be(49.5m);
        low.Value.Should().Be(36.15m);
        high.Value.Should().BeGreaterThan(low.Value);
    }

    [Fact]
    public void ScoreMl_RanksAHigherUpliftFirstAtTheSameSegment()
    {
        var high = PriorityRules.ScoreMl(RfmSegmentNames.Champions, null, NoFacts, 0.30m, PriorityUpliftSegments.Persuadable, Options)!;
        var low = PriorityRules.ScoreMl(RfmSegmentNames.Champions, null, NoFacts, 0.02m, PriorityUpliftSegments.SureThing, Options)!;

        high.Value.Should().BeGreaterThan(low.Value);
    }

    [Fact]
    public void ScoreMl_AddsThePersuadableAndHighUpliftReasonsBeforeTheSegmentReasons()
    {
        var score = PriorityRules.ScoreMl(RfmSegmentNames.AtRisk, null, NoFacts, 0.25m, PriorityUpliftSegments.Persuadable, Options)!;

        score.ReasonCodes.Should().Equal(PriorityReasons.Persuadable, PriorityReasons.UpliftHigh, PriorityReasons.AtRisk);
    }

    [Fact]
    public void ScoreMl_WithoutAPersuadableOrHighUplift_GivesNoUpliftReason()
    {
        var score = PriorityRules.ScoreMl(RfmSegmentNames.Champions, null, NoFacts, 0.01m, PriorityUpliftSegments.SureThing, Options)!;

        score.ReasonCodes.Should().BeEmpty();
    }

    [Fact]
    public void ScoreMl_AddsTheCrmBonusesToTheValue()
    {
        var plain = PriorityRules.ScoreMl(RfmSegmentNames.Champions, null, NoFacts, 0.1m, PriorityUpliftSegments.SureThing, Options)!;
        var withTask = PriorityRules.ScoreMl(
            RfmSegmentNames.Champions,
            null,
            new PriorityFacts(HasDueTask: true, HasStaleDeal: false),
            0.1m,
            PriorityUpliftSegments.SureThing,
            Options)!;

        (withTask.Value - plain.Value).Should().Be(Options.TaskDueBonus);
    }

    [Theory]
    [InlineData(true, true, "ready")]
    [InlineData(true, false, "no_scores")]
    [InlineData(false, true, "disabled")]
    public void Resolve_ReportsWhyTheListIsInItsMode(bool mlEnabled, bool hasRun, string expected) =>
        PriorityMlStatus.Resolve(mlEnabled, "crm_uplift-v1", hasRun).Should().Be(expected);

    [Fact]
    public void Resolve_ReportsNoModelWhenNoVersionIsAvailable()
    {
        PriorityMlStatus.Resolve(mlEnabled: true, activeUpliftVersion: null, hasCompletedUpliftRun: true)
            .Should().Be(PriorityMlStatus.NoModel);
    }
}
