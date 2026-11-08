using AwesomeAssertions;
using Parley.Notifications.Contracts.Email;
using Parley.Notifications.Sending;
using Parley.Notifications.Templates;
using Xunit;

namespace Parley.UnitTests.Notifications.Templates;

public sealed class EmailTemplateTests
{
    [Fact]
    public void EveryTemplateKey_HasSubjectHtmlAndTextForUkAndEn()
    {
        foreach (var templateKey in EmailTemplateStore.TemplateKeys)
        {
            foreach (var culture in EmailTemplateStore.Cultures)
            {
                foreach (var part in EmailTemplateStore.Parts)
                {
                    var content = EmailTemplateStore.Load(templateKey, culture, part);
                    content.Should().NotBeNullOrWhiteSpace(
                        $"{templateKey} must have a {culture} {part} template");
                }
            }
        }
    }

    [Fact]
    public void ResolveCulture_UnknownFallsBackToDefault()
    {
        EmailCultureResolver.Resolve("fr", "en").Should().Be("en");
        EmailCultureResolver.Resolve("", "uk").Should().Be("uk");
        EmailCultureResolver.Resolve("de-DE", "en").Should().Be("en");
    }

    [Fact]
    public void ResolveCulture_KnownCulturesAndRegionalVariantsArePreserved()
    {
        EmailCultureResolver.Resolve("uk", "en").Should().Be("uk");
        EmailCultureResolver.Resolve("en-US", "uk").Should().Be("en");
        EmailCultureResolver.Resolve("uk-UA", "en").Should().Be("uk");
    }

    [Fact]
    public void TemplateKeys_UseConstants()
    {
        EmailTemplateStore.TemplateKeys.Should().Contain(EmailTemplateKey.EmailConfirmation);
        EmailTemplateStore.TemplateKeys.Should().Contain(EmailTemplateKey.PasswordReset);
    }
}
