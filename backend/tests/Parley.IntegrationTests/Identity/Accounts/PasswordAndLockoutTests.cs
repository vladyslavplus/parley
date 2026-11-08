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
/// Password reset: the forgot-password answer does not depend on the address, and a
/// reset ends the existing sessions. Lockout engages after repeated wrong passwords.
/// </summary>
public sealed class PasswordAndLockoutTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ChangedPassword = "Another-password-2";

    private const int LockoutFailures = 5;

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

    [Fact]
    public async Task ForgotPassword_ResponseIsIdentical_ForKnownAndUnknownAddress()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var known = await CreateConfirmedAccountAsync(client, mailpit, token);

        var knownResponse = await ForgotPasswordAsync(client, known.Email, token);
        var unknownResponse = await ForgotPasswordAsync(client, UniqueEmail(), token);

        knownResponse.StatusCode.Should().Be(unknownResponse.StatusCode);
        (await knownResponse.Content.ReadAsStringAsync(token))
            .Should().Be(await unknownResponse.Content.ReadAsStringAsync(token));
    }

    [Fact]
    public async Task ResetPassword_ChangesPassword_AndEndsExistingSessions()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var account = await CreateConfirmedAccountAsync(client, mailpit, token);
        var session = await ReadTokensAsync(await SignInAsync(client, account.Email, Password, token), token);

        (await ForgotPasswordAsync(client, account.Email, token)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        var message = await WaitForEmailAsync(mailpit, account.Email, token, EnResetSubject);
        var (userId, resetToken) = ReadLink(message);

        var reset = await ResetPasswordAsync(client, userId, resetToken, ChangedPassword, token);
        var resetBody = await reset.Content.ReadAsStringAsync(token);
        reset.StatusCode.Should().Be(HttpStatusCode.NoContent, resetBody);

        (await SignInAsync(client, account.Email, Password, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await SignInAsync(client, account.Email, ChangedPassword, token)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await RefreshAsync(client, session.RefreshToken, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RepeatedWrongPasswords_LockAccount_EvenForCorrectPassword()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var account = await CreateConfirmedAccountAsync(client, mailpit, token);

        for (var attempt = 0; attempt < LockoutFailures; attempt++)
        {
            var failed = await SignInAsync(client, account.Email, "wrong-password-1", token);
            failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var locked = await SignInAsync(client, account.Email, Password, token);
        locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await ReadJsonAsync(locked, token);
        body.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.AuthInvalidCredentials);
    }
}
