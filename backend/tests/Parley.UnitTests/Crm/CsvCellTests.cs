using AwesomeAssertions;
using Parley.Crm.Exports;
using Xunit;

namespace Parley.UnitTests.Crm;

/// <summary>A cell a spreadsheet would read as a formula gets a leading apostrophe; quoting follows RFC 4180.</summary>
public sealed class CsvCellTests
{
    [Theory]
    [InlineData("=cmd|' /C calc'!A0", "'=cmd|' /C calc'!A0")]
    [InlineData("+15550100", "'+15550100")]
    [InlineData("-5", "'-5")]
    [InlineData("@SUM(A1:A2)", "'@SUM(A1:A2)")]
    [InlineData("\tindented", "'\tindented")]
    [InlineData(" =HYPERLINK(\"http://example.test\")", "\"' =HYPERLINK(\"\"http://example.test\"\")\"")]
    [InlineData("  -5", "'  -5")]
    [InlineData("\n=cmd", "\"'\n=cmd\"")]
    [InlineData("\r+SUM(A1)", "\"'\r+SUM(A1)\"")]
    [InlineData("＝cmd", "'＝cmd")]
    [InlineData("＋cmd", "'＋cmd")]
    [InlineData("－cmd", "'－cmd")]
    [InlineData("＠SUM(A1)", "'＠SUM(A1)")]
    [InlineData("Price: =5", "Price: =5")]
    [InlineData("Olena", "Olena")]
    [InlineData("Kyiv, Ukraine", "\"Kyiv, Ukraine\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    public void Format_NeutralizesFormulas_AndQuotesSpecialCharacters(string value, string expected)
    {
        CsvCell.Format(value).Should().Be(expected);
    }

    [Fact]
    public void Format_ReturnsEmpty_ForMissingValue()
    {
        CsvCell.Format(null).Should().BeEmpty();
    }
}
