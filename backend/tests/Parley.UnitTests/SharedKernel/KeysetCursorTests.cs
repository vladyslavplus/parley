using AwesomeAssertions;
using Parley.SharedKernel.Paging;
using Xunit;

namespace Parley.UnitTests.SharedKernel;

/// <summary>Keyset cursors carry the sort value and the identifier; malformed input never decodes.</summary>
public sealed class KeysetCursorTests
{
    [Fact]
    public void Encode_RoundTrips_SortKeysWithSeparatorCharacters()
    {
        var cursor = new KeysetCursor("name | with pipe and ünïcode", Guid.CreateVersion7());

        var decoded = KeysetCursor.TryDecode(cursor.Encode(), out var result);

        decoded.Should().BeTrue();
        result.Should().Be(cursor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64 !!")]
    [InlineData("YWJj")]
    public void TryDecode_RejectsMalformedValues(string value)
    {
        KeysetCursor.TryDecode(value, out var result).Should().BeFalse();
        result.Should().BeNull();
    }
}
