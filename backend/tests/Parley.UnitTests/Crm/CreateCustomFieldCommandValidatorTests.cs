using AwesomeAssertions;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Features.CustomFields.Create;
using Xunit;

namespace Parley.UnitTests.Crm;

/// <summary>A select field must come with at least one option, because without one no value can ever be accepted.</summary>
public sealed class CreateCustomFieldCommandValidatorTests
{
    private const string ValidKey = "segment";
    private const string ValidLabel = "Segment";

    private static readonly CreateCustomFieldCommandValidator Validator = new();

    [Fact]
    public void Select_WithoutOptions_IsRejected()
    {
        var command = new CreateCustomFieldCommand(CustomFieldEntityTypes.Contact, ValidKey, ValidLabel, CustomFieldTypes.Select, null, false, 0);

        Validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Select_WithEmptyOptions_IsRejected()
    {
        var command = new CreateCustomFieldCommand(CustomFieldEntityTypes.Contact, ValidKey, ValidLabel, CustomFieldTypes.Select, [], false, 0);

        Validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Select_WithOneOption_IsAccepted()
    {
        var command = new CreateCustomFieldCommand(CustomFieldEntityTypes.Contact, ValidKey, ValidLabel, CustomFieldTypes.Select, ["smb"], false, 0);

        Validator.Validate(command).IsValid.Should().BeTrue();
    }
}
