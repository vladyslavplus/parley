using AwesomeAssertions;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Segments.Shared;
using Xunit;

namespace Parley.UnitTests.Campaigns.Segments;

/// <summary>A segment definition with a null country code or an empty identifier is an input error, not a server error.</summary>
public sealed class SegmentDefinitionValidatorTests
{
    private const string GermanyCode = "DE";

    private readonly SegmentDefinitionValidator _validator = new();

    [Fact]
    public void Validate_RejectsNullCountryCode()
    {
        var definition = new SegmentDefinition { CountryCodes = ["US", null!] };

        _validator.Validate(definition).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsNullList_InsteadOfThrowing()
    {
        var definition = new SegmentDefinition { CountryCodes = null! };

        var result = _validator.Validate(definition);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(SegmentDefinition.CountryCodes));
    }

    [Fact]
    public void Validate_RejectsEmptyOwnerIdentifier()
    {
        var definition = new SegmentDefinition { OwnerUserIds = [Guid.Empty] };

        _validator.Validate(definition).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_AcceptsWellFormedDefinition()
    {
        var definition = new SegmentDefinition { CountryCodes = [GermanyCode] };

        _validator.Validate(definition).IsValid.Should().BeTrue();
    }
}
