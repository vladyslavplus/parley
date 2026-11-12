using AwesomeAssertions;
using Parley.Insights.Contracts.Errors;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Xunit;

namespace Parley.UnitTests.Insights.Scoring;

public sealed class ScoringRunRulesTests
{
    [Fact]
    public void StartBlocker_RefusesInTheOrderOfTheSwitchThenTheModelThenTheActiveRun()
    {
        ScoringRunRules.StartBlocker(mlEnabled: false, activeVersion: null, activeRunExists: true)
            .Should().Be(InsightsErrorCodes.ScoringMlDisabled);
        ScoringRunRules.StartBlocker(mlEnabled: true, activeVersion: null, activeRunExists: true)
            .Should().Be(InsightsErrorCodes.ScoringModelUnavailable);
        ScoringRunRules.StartBlocker(mlEnabled: true, activeVersion: "v1", activeRunExists: true)
            .Should().Be(InsightsErrorCodes.ScoringRunInProgress);
        ScoringRunRules.StartBlocker(mlEnabled: true, activeVersion: "v1", activeRunExists: false).Should().BeNull();
    }

    [Theory]
    [InlineData(10, 100, 0.05, true)]
    [InlineData(5, 100, 0.05, false)]
    [InlineData(0, 0, 0.05, false)]
    public void RejectedShareExceeded_ComparesTheShareOfTheRun(int rejected, int total, decimal maximumShare, bool exceeded) =>
        ScoringRunRules.RejectedShareExceeded(rejected, total, maximumShare).Should().Be(exceeded);

    [Theory]
    [InlineData(404, InsightsErrorCodes.ScoringModelUnavailable)]
    [InlineData(409, InsightsErrorCodes.ScoringModelMismatch)]
    [InlineData(401, InsightsErrorCodes.ScoringServiceRejected)]
    [InlineData(413, InsightsErrorCodes.ScoringServiceRejected)]
    [InlineData(422, InsightsErrorCodes.ScoringServiceRejected)]
    [InlineData(500, InsightsErrorCodes.ScoringServiceUnavailable)]
    public void ErrorCodeOfStatus_MapsEachRefusalToItsStableCode(int status, string code) =>
        ScoringRunRules.ErrorCodeOfStatus(status).Should().Be(code);

    [Fact]
    public void MatchesPinnedModel_RequiresTheSameVersionAndFeatureSchema()
    {
        ScoringRunRules.MatchesPinnedModel("v2", 1, "v2", 1).Should().BeTrue();
        ScoringRunRules.MatchesPinnedModel("v2", 2, "v2", 1).Should().BeFalse();
        ScoringRunRules.MatchesPinnedModel("v3", 1, "v2", 1).Should().BeFalse();
    }

    [Fact]
    public void ModelNameOf_NamesTheTwoScoredKindsAndNoOther()
    {
        ScoringRunRules.ModelNameOf(ScoreKinds.Uplift).Should().Be(ScoringModelNames.Uplift);
        ScoringRunRules.ModelNameOf(ScoreKinds.Clv).Should().Be(ScoringModelNames.Clv);
        ScoringRunRules.ModelNameOf(ScoreKinds.Rfm).Should().BeNull();
    }
}
