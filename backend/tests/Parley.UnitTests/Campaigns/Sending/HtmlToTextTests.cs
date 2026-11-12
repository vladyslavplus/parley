using AwesomeAssertions;
using Parley.Campaigns.Sending.Messages;
using Xunit;

namespace Parley.UnitTests.Campaigns.Sending;

public sealed class HtmlToTextTests
{
    [Fact]
    public void Convert_RemovesTags_DecodesEntitiesOnce_AndKeepsParagraphs()
    {
        var text = HtmlToText.Convert("<p>Hi <b>Ada</b> &amp; team</p><p>Second&nbsp;line<br/>third</p>");

        text.Should().Be("Hi Ada & team\n\nSecond line\nthird");
    }

    [Fact]
    public void Convert_KeepsTheAddressOfAWebLink_SoTheUnsubscribeLinkSurvives()
    {
        var text = HtmlToText.Convert("<p><a href=\"https://app.parley.test/en/unsubscribe?token=abc&amp;x=1\">Unsubscribe</a></p>");

        text.Should().Be("Unsubscribe (https://app.parley.test/en/unsubscribe?token=abc&x=1)");
    }

    [Fact]
    public void Convert_DropsScripts_AndKeepsTheLabelOfANonWebLink()
    {
        var text = HtmlToText.Convert("<script>alert('x')</script><p>Hello <a href=\"javascript:alert(1)\">here</a> &lt;img src=x&gt;</p>");

        text.Should().Be("Hello here <img src=x>");
        text.Should().NotContain("javascript");
    }
}
