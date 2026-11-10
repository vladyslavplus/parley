using AwesomeAssertions;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Campaigns.Update;
using Parley.Campaigns.Features.Segments.Update;
using Parley.Campaigns.Features.Templates.Update;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.UnitTests.Campaigns.Validation;

/// <summary>An update without a row version is a validation error (400), not a version conflict (409).</summary>
public sealed class UpdateVersionValidationTests
{
    private const uint MissingVersion = 0;

    private const uint ReadVersion = 1;

    [Fact]
    public void TemplateUpdate_WithoutVersion_IsInvalid()
    {
        var command = new UpdateTemplateCommand(Guid.NewGuid(), "Welcome", "Hi", "Hello", CultureCodes.English, MissingVersion);

        new UpdateTemplateCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TemplateUpdate_WithVersion_IsValid()
    {
        var command = new UpdateTemplateCommand(Guid.NewGuid(), "Welcome", "Hi", "Hello", CultureCodes.English, ReadVersion);

        new UpdateTemplateCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SegmentUpdate_WithoutVersion_IsInvalid()
    {
        var command = new UpdateSegmentCommand(Guid.NewGuid(), "Germany", new SegmentDefinition(), MissingVersion);

        new UpdateSegmentCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CampaignUpdate_WithoutVersion_IsInvalid()
    {
        var command = new UpdateCampaignCommand(
            Guid.NewGuid(),
            "Spring",
            Guid.NewGuid(),
            null,
            null,
            Parley.Campaigns.Domain.Campaigns.CampaignTargetingModes.All,
            null,
            MissingVersion);

        new UpdateCampaignCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }
}
