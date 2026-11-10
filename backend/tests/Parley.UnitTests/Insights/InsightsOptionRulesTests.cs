using AwesomeAssertions;
using Parley.Insights.Options;
using Xunit;

namespace Parley.UnitTests.Insights;

/// <summary>Startup checks of the insights options: every scored segment weighted, in order, and the ranges held.</summary>
public sealed class InsightsOptionRulesTests
{
    [Fact]
    public void PriorityIsValid_AcceptsAFullNonIncreasingWeightTable()
    {
        InsightsOptionRules.PriorityIsValid(ValidPriority()).Should().BeTrue();
    }

    [Fact]
    public void PriorityIsValid_RejectsAMissingSegmentWeight()
    {
        var options = ValidPriority();
        options.Weights.Remove("lost");

        InsightsOptionRules.PriorityIsValid(options).Should().BeFalse();
    }

    [Fact]
    public void PriorityIsValid_RejectsWeightsThatRiseAlongTheOrder()
    {
        var options = ValidPriority();
        options.Weights["lost"] = 500m;

        InsightsOptionRules.PriorityIsValid(options).Should().BeFalse();
    }

    [Fact]
    public void PriorityIsValid_RejectsAListSizeOutsideTheRange()
    {
        var options = ValidPriority();
        options.ItemsPerOwner = InsightsPriorityOptions.MaximumItemsPerOwner + 1;

        InsightsOptionRules.PriorityIsValid(options).Should().BeFalse();
    }

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(60, true)]
    [InlineData(61, false)]
    public void DashboardIsValid_KeepsTheCacheLifetimeBetweenThirtyAndSixtySeconds(int seconds, bool expected)
    {
        InsightsOptionRules.DashboardIsValid(new InsightsDashboardOptions { CacheSeconds = seconds }).Should().Be(expected);
    }

    private static InsightsPriorityOptions ValidPriority()
    {
        var weights = new Dictionary<string, decimal>();
        var weight = 80m;
        foreach (var segment in InsightsPriorityOptions.ScoredSegmentsInOrder)
        {
            weights[segment] = weight;
            weight -= 10m;
        }

        return new InsightsPriorityOptions
        {
            Weights = weights,
            TaskDueBonus = 15m,
            StaleDealBonus = 10m,
            StaleDealDays = 14,
            ItemsPerOwner = 100,
        };
    }
}
