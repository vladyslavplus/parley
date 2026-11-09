using AwesomeAssertions;
using Parley.Campaigns.Domain.Targeting;
using Xunit;

namespace Parley.UnitTests.Campaigns.Targeting;

/// <summary>Score ranking orders the whole audience and rounds the targeted count down, independent of the input order.</summary>
public sealed class TargetingSelectorTests
{
    private static readonly Guid LowId = Guid.Parse("0192f5a1-0000-7000-8000-000000000001");

    private static readonly Guid MidId = Guid.Parse("0192f5a1-0000-7000-8000-000000000002");

    private static readonly Guid HighId = Guid.Parse("0192f5a1-0000-7000-8000-000000000003");

    private const int HalfShareBps = 5000;

    private const int OneThirdShareBps = 3333;

    private const int AudienceSize = 3;

    [Fact]
    public void Order_RanksHighestScoreFirst_AndBreaksTiesOnContactId()
    {
        var scored = new (Guid ContactId, decimal Score)[]
        {
            (HighId, 0.5m),
            (LowId, 0.2m),
            (MidId, 0.5m),
        };

        TargetingSelector.Order(scored).Should().Equal(MidId, HighId, LowId);
    }

    [Fact]
    public void Order_DoesNotDependOnInputOrder()
    {
        var forward = TargetingSelector.Order(new (Guid, decimal)[] { (LowId, 0.1m), (MidId, 0.4m), (HighId, 0.9m) });
        var reversed = TargetingSelector.Order(new (Guid, decimal)[] { (HighId, 0.9m), (MidId, 0.4m), (LowId, 0.1m) });

        forward.Should().Equal(reversed);
    }

    [Theory]
    [InlineData(AudienceSize, HalfShareBps, 1)]
    [InlineData(AudienceSize, OneThirdShareBps, 0)]
    [InlineData(AudienceSize, 10000, AudienceSize)]
    public void TargetCount_RoundsDownFromTheWholeAudience(int audienceSize, int shareBps, int expected)
    {
        TargetingSelector.TargetCount(audienceSize, shareBps).Should().Be(expected);
    }
}
