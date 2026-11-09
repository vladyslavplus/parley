using AwesomeAssertions;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Deals;
using Xunit;

namespace Parley.UnitTests.Crm.Deals;

/// <summary>Board positions leave gaps between deals; an insert takes the middle, and a stage without room is renumbered.</summary>
public sealed class DealBoardPositionsTests
{
    [Fact]
    public void Between_EmptyStage_UsesOneGap()
    {
        DealBoardPositions.Between(null, null).Should().Be(CrmLimits.DealBoardPositionGap);
    }

    [Fact]
    public void Between_AfterLastDeal_AddsOneGap()
    {
        DealBoardPositions.Between(2048, null).Should().Be(3072);
    }

    [Fact]
    public void Between_BeforeFirstDeal_SubtractsOneGap()
    {
        DealBoardPositions.Between(null, 1024).Should().Be(0);
    }

    [Fact]
    public void Between_TwoNeighbours_TakesTheMiddle()
    {
        DealBoardPositions.Between(1024, 2048).Should().Be(1536);
    }

    [Fact]
    public void Between_AdjacentNeighbours_LeaveNoRoom()
    {
        DealBoardPositions.Between(1024, 1025).Should().BeNull();
    }

    [Fact]
    public void Renumber_SpacesDealsByOneGap_InTheGivenOrder()
    {
        var deals = new List<Deal> { new(), new(), new() };

        DealBoardPositions.Renumber(deals);

        deals.Select(deal => deal.BoardPosition).Should().Equal(1024, 2048, 3072);
    }
}
