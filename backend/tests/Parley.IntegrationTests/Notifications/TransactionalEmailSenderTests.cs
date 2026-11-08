using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.Notifications;
using Parley.Notifications.Contracts.Email;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Notifications;

public sealed class TransactionalEmailSenderTests(MailpitFixture mailpit)
{
    private const string ConfirmationLink = "https://app.parley.test/confirm?token=test-token";
    private const string TestFromAddress = "no-reply@parley.test";
    private const string TestFromName = "Parley";
    private const string UkSubject = "Підтвердіть адресу електронної пошти";
    private const string UkTextFragment = "перейшовши за посиланням";
    private const string EnSubject = "Confirm your email address";
    private const string EnTextFragment = "Confirm your email address using this link";
    private const string HostileDisplayName = "<script>alert('x')</script>";
    private const string EncodedHostileOpeningTag = "&lt;script&gt;";

    [Theory]
    [InlineData(CultureCodes.Ukrainian, UkSubject, UkTextFragment)]
    [InlineData(CultureCodes.English, EnSubject, EnTextFragment)]
    public async Task ConfirmationEmail_SendsLocalizedMultipartMessage(
        string culture,
        string expectedSubject,
        string expectedTextFragment)
    {
        var token = TestContext.Current.CancellationToken;
        var recipient = $"confirm-{culture}-{Guid.CreateVersion7():N}@parley.test";
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ITransactionalEmailSender>();

        var result = await sender.SendAsync(
            recipient,
            EmailTemplateKey.EmailConfirmation,
            culture,
            EmailTemplateVariables.From(EmailVariableNames.ConfirmationLink, ConfirmationLink),
            token);

        result.IsSuccess.Should().BeTrue();
        var captured = await mailpit.FindMessageAsync(recipient, token);
        captured.Should().NotBeNull();
        captured!.Subject.Should().Be(expectedSubject);
        captured.ToAddresses.Should().Contain(recipient);
        captured.Text.Should().Contain(expectedTextFragment, "the plain-text part is present and localized");
        captured.Text.Should().Contain(ConfirmationLink);
        captured.Html.Should().Contain(ConfirmationLink);
    }

    [Fact]
    public async Task ConfirmationEmail_EncodesHostileValueInHtmlPart()
    {
        var token = TestContext.Current.CancellationToken;
        var recipient = $"hostile-{Guid.CreateVersion7():N}@parley.test";
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ITransactionalEmailSender>();

        var result = await sender.SendAsync(
            recipient,
            EmailTemplateKey.EmailConfirmation,
            CultureCodes.English,
            EmailTemplateVariables.From(
                EmailVariableNames.ConfirmationLink,
                ConfirmationLink,
                (EmailVariableNames.DisplayName, HostileDisplayName)),
            token);

        result.IsSuccess.Should().BeTrue();
        var captured = await mailpit.FindMessageAsync(recipient, token);
        captured.Should().NotBeNull();
        captured!.Html.Should().Contain(EncodedHostileOpeningTag);
        captured.Html.Should().NotContain("<script>");
    }

    private ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Host"] = mailpit.Host,
                ["Email:Port"] = mailpit.SmtpPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Email:FromAddress"] = TestFromAddress,
                ["Email:FromName"] = TestFromName,
                ["Email:SecureSocketOption"] = "None",
                ["Email:Timeout"] = "00:00:30",
                ["Email:DefaultCulture"] = CultureCodes.English,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddNotifications(configuration);
        return services.BuildServiceProvider();
    }
}
