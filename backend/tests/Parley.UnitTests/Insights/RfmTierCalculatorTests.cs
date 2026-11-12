using AwesomeAssertions;
using Parley.Insights.Domain.Rfm;
using Xunit;

namespace Parley.UnitTests.Insights;

/// <summary>The quintile tiers match SQL ntile(5), including the uneven split, tiny sets and ties.</summary>
public sealed class RfmTierCalculatorTests
{
    [Fact]
    public void Ntile_SplitsTenRowsIntoFivePairs()
    {
        var buckets = Enumerable.Range(0, 10).Select(index => RfmTierCalculator.Ntile(index, 10, 5)).ToArray();

        buckets.Should().Equal(1, 1, 2, 2, 3, 3, 4, 4, 5, 5);
    }

    [Fact]
    public void Ntile_GivesTheFirstRemainderBucketsOneExtraRow()
    {
        // Seven rows: sizes 2, 2, 1, 1, 1, as ntile(5) does.
        var buckets = Enumerable.Range(0, 7).Select(index => RfmTierCalculator.Ntile(index, 7, 5)).ToArray();

        buckets.Should().Equal(1, 1, 2, 2, 3, 4, 5);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void Ntile_WithFewerRowsThanBuckets_GivesEachRowItsOwnTier(int count)
    {
        var buckets = Enumerable.Range(0, count).Select(index => RfmTierCalculator.Ntile(index, count, 5)).ToArray();

        buckets.Should().Equal(Enumerable.Range(1, count));
    }

    [Fact]
    public void Compute_RanksRecencyTheOtherWay_SoTheMostRecentIsTierFive()
    {
        var contacts = new[]
        {
            Input(1, recencyDays: 400, orders: 1, value: 10m),
            Input(2, recencyDays: 200, orders: 2, value: 20m),
            Input(3, recencyDays: 5, orders: 3, value: 30m),
        };

        var tiers = RfmTierCalculator.Compute(contacts);

        tiers[Id(3)].Should().Be(new RfmTiers(3, 3, 3));
        tiers[Id(1)].Should().Be(new RfmTiers(1, 1, 1));
    }

    [Fact]
    public void Compute_BreaksTiesByContactId_AndDoesNotDependOnInputOrder()
    {
        var contacts = Enumerable.Range(1, 5)
            .Select(index => Input(index, recencyDays: 30, orders: 2, value: 100m))
            .ToList();

        var forward = RfmTierCalculator.Compute(contacts);
        var backward = RfmTierCalculator.Compute(contacts.AsEnumerable().Reverse().ToList());

        backward.Should().BeEquivalentTo(forward);
        forward[Id(1)].F.Should().Be(1, "the lowest contact id takes the lowest tier among equal values");
        forward[Id(5)].F.Should().Be(5);
    }

    private static RfmInput Input(int index, int recencyDays, int orders, decimal value) =>
        new(Id(index), null, recencyDays, orders, value);

    private static Guid Id(int index) => new($"00000000-0000-0000-0000-{index:D12}");
}
