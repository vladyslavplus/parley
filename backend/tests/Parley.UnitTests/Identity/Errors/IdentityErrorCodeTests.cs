using System.Globalization;
using System.Reflection;
using System.Resources;
using AwesomeAssertions;
using Parley.Identity.Contracts.Errors;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.UnitTests.Identity.Errors;

public sealed class IdentityErrorCodeTests
{
    [Fact]
    public void EveryIdentityErrorCode_HasUkrainianAndEnglishTexts()
    {
        var codes = typeof(IdentityErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetValue(null)!)
            .ToArray();
        codes.Should().NotBeEmpty();

        var manager = new ResourceManager(ErrorTextResources.BaseName, typeof(ErrorCodes).Assembly);
        var english = CultureInfo.GetCultureInfo(CultureCodes.English);
        var ukrainian = CultureInfo.GetCultureInfo(CultureCodes.Ukrainian);

        foreach (var code in codes)
        {
            manager.GetString(code, english).Should().NotBeNullOrWhiteSpace($"code {code} needs an English entry");
            manager.GetString(code, ukrainian).Should().NotBeNullOrWhiteSpace($"code {code} needs a Ukrainian entry");
        }
    }
}
