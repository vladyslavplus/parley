using System.Net;
using AwesomeAssertions;
using Parley.Identity.Contracts.Errors;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;
using static Parley.IntegrationTests.Infrastructure.Auth.AuthTestClient;

namespace Parley.IntegrationTests.Identity.Accounts;

/// <summary>
/// Registration through the emailed link, in the requested language, with sign-in
/// refused until confirmation and the link usable exactly once.
/// </summary>
public sealed class AccountLifecycleTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string UkConfirmationSubject = "Підтвердіть адресу електронної пошти";

    private const string EnConfirmationSubject = "Confirm your email address";

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(postgres, mailpit);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData(CultureCodes.Ukrainian, UkConfirmationSubject)]
    [InlineData(CultureCodes.English, EnConfirmationSubject)]
    public async Task Registration_EmailsLinkInRequestedLanguage_AndLinkWorksOnce(string language, string expectedSubject)
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, language);
        var email = UniqueEmail();

        var registered = await RegisterAsync(client, email, Password, token);
        registered.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var message = await WaitForEmailAsync(mailpit, email, token);
        message.Subject.Should().Be(expectedSubject);
        var (userId, confirmationToken) = ReadLink(message);

        var refused = await SignInAsync(client, email, Password, token);
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var refusedBody = await ReadJsonAsync(refused, token);
        refusedBody.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.AuthEmailNotConfirmed);

        var confirmed = await ConfirmAsync(client, userId, confirmationToken, token);
        confirmed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var reused = await ConfirmAsync(client, userId, confirmationToken, token);
        reused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var reusedBody = await ReadJsonAsync(reused, token);
        reusedBody.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.AuthTokenInvalid);

        var signedIn = await SignInAsync(client, email, Password, token);
        signedIn.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await ReadTokensAsync(signedIn, token);

        // Sensitive values never reach the host logs.
        var logs = string.Join('\n', _factory.Logs.Entries);
        logs.Should().NotContain(confirmationToken);
        logs.Should().NotContain(tokens.RefreshToken);
        logs.Should().NotContain(tokens.AccessToken);
        logs.Should().NotContain(Password);
    }
}
