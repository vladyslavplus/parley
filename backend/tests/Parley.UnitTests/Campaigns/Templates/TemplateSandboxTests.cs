using System.Text.Encodings.Web;
using AwesomeAssertions;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Shared;
using Parley.Campaigns.Templates;
using Parley.Campaigns.Templates.Model;
using Xunit;

namespace Parley.UnitTests.Campaigns.Templates;

/// <summary>The template sandbox accepts only the allow-listed variables and tags, and escapes values it prints.</summary>
public sealed class TemplateSandboxTests
{
    private const string EmailName = "contact.email";

    private const string LineParameter = "line";

    private readonly TemplateSandbox _sandbox = new();

    [Fact]
    public void Parse_ReportsSyntaxErrorWithLine()
    {
        var result = _sandbox.Parse("Hello\n{{ contact.first_name | }}", CampaignsLimits.TemplateBodyMaximumLength);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CampaignsErrorCodes.TemplateSyntaxInvalid);
        result.Error.Parameters![LineParameter].Should().Be("2");
    }

    [Fact]
    public void Parse_RejectsUnknownVariable_EvenInBranchThatNeverRuns()
    {
        var result = _sandbox.Parse("{% if false %}{{ contact.email }}{% endif %}", CampaignsLimits.TemplateBodyMaximumLength);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CampaignsErrorCodes.TemplateVariableUnknown);
        result.Error.Parameters!["name"].Should().Be(EmailName);
    }

    [Theory]
    [InlineData("{% include 'header' %}", "include")]
    [InlineData("{% render 'card' %}", "render")]
    [InlineData("{% from 'macros' %}", "from")]
    [InlineData("{% liquid echo 'x' %}", "liquid")]
    public void Parse_RejectsTagsThatReadFilesOrOtherTemplates(string source, string tagName)
    {
        var result = _sandbox.Parse(source, CampaignsLimits.TemplateBodyMaximumLength);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CampaignsErrorCodes.TemplateTagNotAllowed);
        result.Error.Parameters!["name"].Should().Be(tagName);
    }

    [Fact]
    public void Parse_AcceptsAllowListedVariablesAndAllowListedFilters()
    {
        var result = _sandbox.Parse(
            "{% for item in contact.first_name %}{{ item | default: 'there' }}{% endfor %} {{ company.name }} {{ unsubscribe_url }}",
            CampaignsLimits.TemplateBodyMaximumLength);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Parse_RejectsSourceOverTheLengthLimit()
    {
        var result = _sandbox.Parse(new string('a', 11), maximumLength: 10);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CampaignsErrorCodes.TemplateTooLong);
    }

    [Fact]
    public async Task RenderAsync_EscapesHtmlInValuesButNotInTemplateText()
    {
        var template = _sandbox.Parse("<p>Hi {{ contact.first_name }}</p>", CampaignsLimits.TemplateBodyMaximumLength).Value!;
        var model = new TemplateModel(new ContactTemplateModel("<script>x</script>", null), new CompanyTemplateModel(null), "https://example.test/u");

        var rendered = await _sandbox.RenderAsync(template, model, HtmlEncoder.Default, TestContext.Current.CancellationToken);

        rendered.IsSuccess.Should().BeTrue();
        rendered.Value.Should().Be("<p>Hi &lt;script&gt;x&lt;/script&gt;</p>");
    }
}
