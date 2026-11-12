using AwesomeAssertions;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Domain.Priority;
using Xunit;

namespace Parley.UnitTests.Insights;

/// <summary>The ML mode of the priority rule: the uplift ranking, the do-not-disturb exclusion, the reasons and the status.</summary>
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
    public void ScoreMl_LeavesOutADoNotDisturbContact()
    {
        var score = PriorityRules.ScoreMl(RfmSegmentNames.Champions, null, NoFacts, 0.9m, PriorityUpliftSegments.DoNotDisturb, Options);

        score.Should().BeNull();
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
