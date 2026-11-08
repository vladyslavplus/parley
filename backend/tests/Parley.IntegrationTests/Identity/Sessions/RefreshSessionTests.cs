using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Npgsql;
using Parley.Identity.Contracts.Errors;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;
using static Parley.IntegrationTests.Infrastructure.Auth.AuthTestClient;

namespace Parley.IntegrationTests.Identity.Sessions;

/// <summary>
/// Refresh rotation, the reuse grace window, family revocation, hashed storage, and
/// acceptance of the issued access token by a protected endpoint.
/// </summary>
public sealed class RefreshSessionTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private static readonly TimeSpan WithinGrace = TimeSpan.FromSeconds(10);

    private static readonly TimeSpan AfterGrace = TimeSpan.FromSeconds(31);

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
    public async Task Refresh_ReturnsNewPair_AndStoresOnlyHashes()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var email = (await CreateConfirmedAccountAsync(client, mailpit, token)).Email;
        var signedIn = await ReadTokensAsync(await SignInAsync(client, email, Password, token), token);

        var refreshed = await RefreshAsync(client, signedIn.RefreshToken, token);

        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await ReadTokensAsync(refreshed, token);
        rotated.RefreshToken.Should().NotBe(signedIn.RefreshToken);

        var stored = (await ReadStoredHashesAsync(token)).Select(Convert.ToHexString).ToArray();
        stored.Should().Contain(Convert.ToHexString(Sha256(signedIn.RefreshToken)));
        stored.Should().Contain(Convert.ToHexString(Sha256(rotated.RefreshToken)));
        stored.Should().NotContain(Convert.ToHexString(Encoding.UTF8.GetBytes(signedIn.RefreshToken)));
        stored.Should().NotContain(Convert.ToHexString(Encoding.UTF8.GetBytes(rotated.RefreshToken)));
    }

    [Fact]
    public async Task Refresh_ReusedWithinGrace_ReturnsNewPair()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var email = (await CreateConfirmedAccountAsync(client, mailpit, token)).Email;
        var original = await ReadTokensAsync(await SignInAsync(client, email, Password, token), token);
        (await RefreshAsync(client, original.RefreshToken, token)).StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.Clock.Advance(WithinGrace);
        var racedRefresh = await RefreshAsync(client, original.RefreshToken, token);

        racedRefresh.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_OldTokenWithinGraceAfterLogout_IsRejected()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var email = (await CreateConfirmedAccountAsync(client, mailpit, token)).Email;
        var original = await ReadTokensAsync(await SignInAsync(client, email, Password, token), token);
        var successor = await ReadTokensAsync(await RefreshAsync(client, original.RefreshToken, token), token);
        (await SignOutAsync(client, successor.RefreshToken, token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        _factory.Clock.Advance(WithinGrace);
        var revivedAttempt = await RefreshAsync(client, original.RefreshToken, token);

        revivedAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(client, successor.RefreshToken, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_OldTokenWithinGraceAfterPasswordReset_IsRejected()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var account = await CreateConfirmedAccountAsync(client, mailpit, token);
        var original = await ReadTokensAsync(await SignInAsync(client, account.Email, Password, token), token);
        var successor = await ReadTokensAsync(await RefreshAsync(client, original.RefreshToken, token), token);

        (await ForgotPasswordAsync(client, account.Email, token)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        var (userId, resetToken) = ReadLink(await WaitForEmailAsync(mailpit, account.Email, token, EnResetSubject));
        (await ResetPasswordAsync(client, userId, resetToken, Password + "-2", token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        _factory.Clock.Advance(WithinGrace);
        var revivedAttempt = await RefreshAsync(client, original.RefreshToken, token);

        revivedAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(client, successor.RefreshToken, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReusedAfterGrace_RevokesFamily()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var email = (await CreateConfirmedAccountAsync(client, mailpit, token)).Email;
        var original = await ReadTokensAsync(await SignInAsync(client, email, Password, token), token);
        var successor = await ReadTokensAsync(await RefreshAsync(client, original.RefreshToken, token), token);

        _factory.Clock.Advance(AfterGrace);
        var reused = await RefreshAsync(client, original.RefreshToken, token);

        reused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var reusedBody = await ReadJsonAsync(reused, token);
        reusedBody.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.AuthRefreshTokenReused);

        var successorAfterRevocation = await RefreshAsync(client, successor.RefreshToken, token);
        successorAfterRevocation.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessToken_FromSignIn_AuthenticatesProtectedRequest()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var account = await CreateConfirmedAccountAsync(client, mailpit, token);
        var signedIn = await ReadTokensAsync(await SignInAsync(client, account.Email, Password, token), token);

        using var request = new HttpRequestMessage(HttpMethod.Get, TestOnlyRoutes.CurrentPrincipal);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", signedIn.AccessToken);
        var response = await client.SendAsync(request, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response, token);
        body.GetProperty("subject").GetString().Should().Be(account.UserId.ToString());
        body.GetProperty("organizationId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private async Task<IReadOnlyList<byte[]>> ReadStoredHashesAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT token_hash FROM identity.refresh_tokens", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var hashes = new List<byte[]>();
        while (await reader.ReadAsync(cancellationToken))
        {
            hashes.Add((byte[])reader.GetValue(0));
        }

        return hashes;
    }

    private static byte[] Sha256(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
