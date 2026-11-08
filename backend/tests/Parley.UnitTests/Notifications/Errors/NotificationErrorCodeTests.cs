using System.Globalization;
using System.Reflection;
using System.Resources;
using AwesomeAssertions;
using Parley.Notifications.Contracts.Errors;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.UnitTests.Notifications.Errors;

public sealed class NotificationErrorCodeTests
{
    [Fact]
    public void EveryNotificationErrorCode_HasUkrainianAndEnglishTexts()
    {
        var codes = typeof(NotificationErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetValue(null)!)
            .ToArray();
        codes.Should().NotBeEmpty();

        var manager = new ResourceManager(
            ErrorTextResources.BaseName,
            typeof(ErrorCodes).Assembly);

        var english = CultureInfo.GetCultureInfo(CultureCodes.English);
        var ukrainian = CultureInfo.GetCultureInfo(CultureCodes.Ukrainian);

        foreach (var code in codes)
        {
            manager.GetString(code, english).Should().NotBeNullOrWhiteSpace($"code {code} needs an English entry");
            manager.GetString(code, ukrainian).Should().NotBeNullOrWhiteSpace($"code {code} needs a Ukrainian entry");
            manager.GetString(code, english).Should().NotBe(code);
            manager.GetString(code, ukrainian).Should().NotBe(code);
        }
    }
}
