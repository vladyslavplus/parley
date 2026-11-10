using System.Text.Encodings.Web;
using AwesomeAssertions;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Shared;
using Parley.Campaigns.Templates;
using Parley.Campaigns.Templates.Model;
using Xunit;

namespace Parley.UnitTests.Campaigns.Templates;

/// <summary>
/// The sandbox limits: no tag or filter outside the allow-lists, a tag cannot hide inside a string literal, and a render
/// stops at the output limit instead of building the text.
/// </summary>
public sealed class TemplateSandboxLimitsTests
{
    private const int OutputLimitForTest = 10;

    private const int OverLimitLength = OutputLimitForTest + 1;

    /// <summary>A body close to the source limit: each loop pass writes it, so a few passes pass the output limit.</summary>
    private const int LargeBodyLength = 99_000;

    private readonly TemplateSandbox _sandbox = new();

    /// <summary>Doubles a value on every step; with assign it grows without the output check ever seeing it.</summary>
    private const string DoublingTemplate =
        "{% assign s = \"xxxxxxxxxx\" %}{% assign s = s | append: s %}{% assign s = s | append: s %}{{ s | size }}";

    [Fact]
    public void Parse_RejectsAssignThatWouldDoubleAValue()
    {
        var result = _sandbox.Parse(DoublingTemplate, CampaignsLimits.TemplateBodyMaximumLength);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CampaignsErrorCodes.TemplateTagNotAllowed);
    }

    [Fact]
    public void Parse_RejectsRawFilter_BecauseItSwitchesEncodingOff()
    {
        var result = _sandbox.Parse("{{ contact.first_name | raw }}", CampaignsLimits.TemplateBodyMaximumLength);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CampaignsErrorCodes.TemplateFilterNotAllowed);
    }

    [Fact]
    public void Parse_RejectsTagHiddenInsideAStringLiteral()
    {
        var result = _sandbox.Parse("{{ \"{%\" }}{% include 'x' %}", CampaignsLimits.TemplateBodyMaximumLength);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CampaignsErrorCodes.TemplateTagNotAllowed);
    }

    [Fact]
    public async Task RenderAsync_StopsALoopThatWouldWriteMoreThanTheOutputLimit()
    {
        var source = "{% for i in (1..5000) %}" + new string('a', LargeBodyLength) + "{% endfor %}";
        var template = _sandbox.Parse(source, CampaignsLimits.TemplateBodyMaximumLength).Value!;
        var model = new TemplateModel(new ContactTemplateModel("Ada", "Lovelace"), new CompanyTemplateModel(null), "https://example.test/u");

        var rendered = await _sandbox.RenderAsync(template, model, HtmlEncoder.Default, TestContext.Current.CancellationToken);

        rendered.IsSuccess.Should().BeFalse();
        rendered.Error!.Code.Should().Be(CampaignsErrorCodes.TemplateRenderFailed);
    }

    [Fact]
    public void OutputWriter_StopsTheRenderAtTheLimit()
    {
        var writer = new TemplateOutputWriter(OutputLimitForTest);
        writer.Write(new string('a', OutputLimitForTest));

        var act = () => writer.Write(new string('b', OverLimitLength - OutputLimitForTest));

        act.Should().Throw<TemplateOutputLimitException>();
        writer.ToString().Should().HaveLength(OutputLimitForTest);
    }
}
