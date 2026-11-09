using AwesomeAssertions;
using Parley.Crm.Shared.Pipelines;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.UnitTests.Crm.Pipelines;

/// <summary>An organization event written without a locale falls back to the platform default; a given locale is kept.</summary>
public sealed class OrganizationLocalesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingLocale_FallsBackToThePlatformDefault(string? locale) =>
        OrganizationLocales.OrDefault(locale).Should().Be(CultureCodes.Default);

    [Fact]
    public void GivenLocale_IsKept() =>
        OrganizationLocales.OrDefault(CultureCodes.Ukrainian).Should().Be(CultureCodes.Ukrainian);
}
