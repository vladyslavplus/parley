using AwesomeAssertions;
using Parley.SharedKernel.Paging;
using Xunit;

namespace Parley.UnitTests.SharedKernel.Paging;

public sealed class PageLimitsTests
{
    [Theory]
    [InlineData(null, 20)]
    [InlineData(0, 20)]
    [InlineData(-5, 20)]
    [InlineData(50, 50)]
    [InlineData(500, 100)]
    public void Resolve_UsesTheDefaultWhenMissingAndCapsAtTheMaximum(int? requested, int expected) =>
        PageLimits.Resolve(requested).Should().Be(expected);
}
