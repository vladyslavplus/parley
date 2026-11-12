using AwesomeAssertions;
using Parley.Insights.Domain.Rfm;
using Xunit;

namespace Parley.UnitTests.Insights;

/// <summary>One case per branch of the segment table, in table order, so a reordered rule breaks a named case.</summary>
public sealed class RfmSegmentRulesTests
{
    [Theory]
    [InlineData(5, 5, 5, "champions")]
    [InlineData(4, 4, 4, "champions")]
    [InlineData(4, 4, 3, "loyal")]
    [InlineData(3, 4, 1, "loyal")]
    [InlineData(4, 2, 1, "promising")]
    [InlineData(5, 1, 5, "promising")]
    [InlineData(3, 3, 1, "needs_attention")]
    [InlineData(2, 3, 1, "at_risk")]
    [InlineData(2, 2, 3, "hibernating")]
    [InlineData(1, 1, 1, "lost")]
    [InlineData(1, 5, 5, "at_risk")]
    [InlineData(3, 1, 1, "regular")]
    [InlineData(2, 2, 2, "regular")]
    [InlineData(4, 3, 3, "regular")]
    public void Classify_ReturnsTheFirstMatchingSegment(int recency, int frequency, int monetary, string expected)
    {
        RfmSegmentRules.Classify(new RfmTiers(recency, frequency, monetary)).Should().Be(expected);
    }

    [Fact]
    public void Classify_WithoutTiers_IsNoOrders()
    {
        RfmSegmentRules.Classify((RfmTiers?)null).Should().Be("no_orders");
    }

    [Fact]
    public void ScoreValue_CombinesTheTiersWithRecencyFirst()
    {
        RfmScoring.ScoreValue(new RfmTiers(5, 4, 3)).Should().Be(543m);
    }

    [Fact]
    public void RecencyDays_AreWholeDaysAndNeverNegative()
    {
        var asOf = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);

        RfmRecency.DaysBetween(asOf, new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero)).Should().Be(1);
        RfmRecency.DaysBetween(asOf, new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero)).Should().Be(0);
    }
}
