using AwesomeAssertions;
using Parley.Crm.Contracts.Metrics;
using Parley.Insights.Features.Scoring.Run;
using Xunit;

namespace Parley.UnitTests.Insights.Scoring;

public sealed class ScoringFeatureMapperTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Uplift_MeasuresRecencyAndTenureFromTheAsOfInstant()
    {
        var row = Row(
            firstOrderAt: AsOf.AddDays(-610),
            lastOrderAt: AsOf.AddDays(-43),
            purchaseDays: 5,
            orders90: 1,
            orders365: 4,
            orderCount: 9,
            value365: 812.5m,
            averageOrder: 203.1m,
            hasOpenDeal: true);

        var features = ScoringFeatureMapper.Uplift(row, AsOf);

        features.RecencyDays.Should().Be(43);
        features.TenureDays.Should().Be(610);
        features.Orders90Days.Should().Be(1);
        features.Orders365Days.Should().Be(4);
        features.OrderCount.Should().Be(9);
        features.Value365Days.Should().Be(812.5m);
        features.AvgOrderValue.Should().Be(203.1m);
        features.HasOpenDeal.Should().Be(1);
    }

    [Fact]
    public void Clv_UsesTheBgNbdConventionAndZeroMoneyWithoutARepeatPurchase()
    {
        var single = Row(
            firstOrderAt: AsOf.AddDays(-100),
            lastOrderAt: AsOf.AddDays(-100),
            purchaseDays: 1,
            averageRepeatOrder: 250m);

        var clv = ScoringFeatureMapper.Clv(single, AsOf);

        clv.Frequency.Should().Be(0);
        clv.RecencyDaysSinceFirst.Should().Be(0);
        clv.TenureDays.Should().Be(100);
        clv.MonetaryValue.Should().Be(0m);
    }

    [Fact]
    public void Clv_CountsRepeatDaysFromFirstToLastOrder()
    {
        var repeat = Row(
            firstOrderAt: AsOf.AddDays(-300),
            lastOrderAt: AsOf.AddDays(-30),
            purchaseDays: 4,
            averageRepeatOrder: 120m);

        var clv = ScoringFeatureMapper.Clv(repeat, AsOf);

        clv.Frequency.Should().Be(3);
        clv.RecencyDaysSinceFirst.Should().Be(270);
        clv.TenureDays.Should().Be(300);
        clv.MonetaryValue.Should().Be(120m);
    }

    [Fact]
    public void HasOrders_IsFalseForAContactWithoutAnOrder()
    {
        ScoringFeatureMapper.HasOrders(Row(firstOrderAt: null, lastOrderAt: null)).Should().BeFalse();
        ScoringFeatureMapper.HasOrders(Row(firstOrderAt: AsOf, lastOrderAt: AsOf)).Should().BeTrue();
    }

    private static CrmCustomerMetricsRow Row(
        DateTimeOffset? firstOrderAt,
        DateTimeOffset? lastOrderAt,
        int purchaseDays = 0,
        int orders90 = 0,
        int orders365 = 0,
        int orderCount = 0,
        decimal value365 = 0m,
        decimal averageOrder = 0m,
        decimal averageRepeatOrder = 0m,
        bool hasOpenDeal = false) => new(
            Guid.NewGuid(),
            null,
            firstOrderAt,
            lastOrderAt,
            orderCount,
            purchaseDays,
            orders90,
            orders365,
            value365,
            value365,
            averageOrder,
            averageRepeatOrder,
            hasOpenDeal);
}
