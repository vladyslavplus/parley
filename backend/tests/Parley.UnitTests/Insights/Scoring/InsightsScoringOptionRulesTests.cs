using AwesomeAssertions;
using Parley.Insights.Options;
using Xunit;

namespace Parley.UnitTests.Insights.Scoring;

public sealed class InsightsScoringOptionRulesTests
{
    [Fact]
    public void ScoringIsValid_AcceptsTheDefaultsWithABaseAddress()
    {
        InsightsOptionRules.ScoringIsValid(Valid()).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://127.0.0.1/")]
    [InlineData("not a uri")]
    public void ScoringIsValid_RefusesABaseAddressThatIsNotHttp(string baseUrl)
    {
        var options = Valid();
        options.BaseUrl = baseUrl;

        InsightsOptionRules.ScoringIsValid(options).Should().BeFalse();
    }

    [Fact]
    public void ScoringIsValid_RefusesATotalTimeoutShorterThanOneAttempt()
    {
        var options = Valid();
        options.AttemptTimeoutSeconds = 20;
        options.TotalTimeoutSeconds = 10;

        InsightsOptionRules.ScoringIsValid(options).Should().BeFalse();
    }

    [Fact]
    public void ScoringIsValid_RefusesABatchAboveTheContractLimit()
    {
        var options = Valid();
        options.BatchSize = InsightsScoringOptions.MaximumBatchSize + 1;

        InsightsOptionRules.ScoringIsValid(options).Should().BeFalse();
    }

    [Fact]
    public void ScoringIsValid_RefusesARejectedShareOutsideZeroToOne()
    {
        var options = Valid();
        options.MaximumRejectedShare = 1.5m;

        InsightsOptionRules.ScoringIsValid(options).Should().BeFalse();
    }

    [Fact]
    public void ScoringIsValid_RefusesAHorizonAboveTheContractLimit()
    {
        var options = Valid();
        options.ClvHorizonDays = InsightsScoringOptions.MaximumHorizonDays + 1;

        InsightsOptionRules.ScoringIsValid(options).Should().BeFalse();
    }

    private static InsightsScoringOptions Valid() => new()
    {
        BaseUrl = "http://127.0.0.1:8090/",
        ApiKey = string.Empty,
    };
}
