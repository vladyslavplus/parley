using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Auth;
using Parley.Identity.Contracts.Errors;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;
using static Parley.IntegrationTests.Infrastructure.Auth.AuthTestClient;

namespace Parley.IntegrationTests.Api;

/// <summary>
/// A rate-limited endpoint answers 429 with the shared ProblemDetails shape and a
/// Retry-After header. Forwarded client addresses are honored only from trusted proxies.
/// </summary>
public sealed class RateLimitingTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string LoginPermitKey = "RateLimiting:Login:PermitLimit";

    private const string TwoPermits = "2";

    private const string ProblemJson = "application/problem+json";

    private const string KnownProxyKey = "TrustedProxies:KnownProxies:0";

    private const string ForwardedForHeader = "X-Forwarded-For";

    /// <summary>Socket address of the requests in these tests: a reverse proxy in front of the API.</summary>
    private const string ProxyPeerAddress = "10.0.0.5";

    private const string UnlistedProxy = "10.20.30.40";

    private const string FirstClientAddress = "203.0.113.10";

    private const string SecondClientAddress = "203.0.113.20";

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(
            postgres,
            mailpit,
            new Dictionary<string, string?> { [LoginPermitKey] = TwoPermits });
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Login_OverLimit_Returns429WithStandardProblemDetails()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CreateClient(_factory, CultureCodes.English);
        var address = UniqueEmail();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            (await SignInAsync(client, address, Password, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var limited = await SignInAsync(client, address, Password, token);

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Content.Headers.ContentType!.MediaType.Should().Be(ProblemJson);
        limited.Headers.Contains("Retry-After").Should().BeTrue();
        var body = await ReadJsonAsync(limited, token);
        body.GetProperty("code").GetString().Should().Be(IdentityErrorCodes.AuthRateLimited);
        body.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_BehindTrustedProxy_EachForwardedClientGetsOwnQuota()
    {
        var token = TestContext.Current.CancellationToken;
        using var proxied = new IdentityApiFactory(postgres, mailpit, ProxyConfiguration(ProxyPeerAddress), IPAddress.Parse(ProxyPeerAddress));
        using var client = CreateClient(proxied, CultureCodes.English);
        var address = UniqueEmail();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            (await SignInFromAsync(client, FirstClientAddress, address, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await SignInFromAsync(client, FirstClientAddress, address, token)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await SignInFromAsync(client, SecondClientAddress, address, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ForwardedHeaderFromUntrustedSender_IsIgnored()
    {
        var token = TestContext.Current.CancellationToken;
        using var untrusted = new IdentityApiFactory(postgres, mailpit, ProxyConfiguration(UnlistedProxy), IPAddress.Parse(ProxyPeerAddress));
        using var client = CreateClient(untrusted, CultureCodes.English);
        var address = UniqueEmail();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            (await SignInFromAsync(client, FirstClientAddress, address, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await SignInFromAsync(client, SecondClientAddress, address, token)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static Dictionary<string, string?> ProxyConfiguration(string proxyAddress) => new()
    {
        [LoginPermitKey] = TwoPermits,
        [KnownProxyKey] = proxyAddress,
    };

    private static async Task<HttpResponseMessage> SignInFromAsync(
        HttpClient client,
        string forwardedFor,
        string email,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Route(AuthRoutes.SignIn))
        {
            Content = JsonContent.Create(new { email, password = Password }),
        };
        request.Headers.Add(ForwardedForHeader, forwardedFor);
        return await client.SendAsync(request, cancellationToken);
    }
}
