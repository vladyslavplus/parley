using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Parley.Api;
using Parley.Api.Idempotency;
using Parley.IntegrationTests.Idempotency.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Idempotency;
using Parley.SharedKernel.Idempotency.Abstractions;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Localization;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Implementations;
using Xunit;
using HeaderNames = Microsoft.Net.Http.Headers.HeaderNames;

namespace Parley.IntegrationTests.Idempotency;

/// <summary>
/// Proves <c>Idempotency-Key</c> replay, the 409 mismatch path, single
/// execution under concurrent first use and per-organization isolation over
/// HTTP against a real Postgres database, using the test-only DbContext for
/// the table mapping.
///
/// One factory (and host) serves the whole class: execution counts are
/// asserted as deltas over a baseline. A unique queue keeps this host's
/// Hangfire server away from other hosts' jobs.
/// </summary>
public sealed class IdempotencyTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly IdempotencyProbeRecorder Recorder = new();

    private static WebApplicationFactory<Program>? _factory;

    private WebApplicationFactory<Program> Factory => _factory ??= BuildFactory(fixture);

    public async ValueTask InitializeAsync() =>
        await fixture.ResetDataAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SameKeySameBody_ReplaysStoredResponse()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var key = Guid.NewGuid().ToString("N");
        var baseline = Recorder.Count;
        using var client = Client(organizationId);

        var first = await PostAsync(client, key, "{\"value\":1}", token);
        var second = await PostAsync(client, key, "{\"value\":1}", token);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadAsStringAsync(token);
        var secondBody = await second.Content.ReadAsStringAsync(token);
        secondBody.Should().Be(firstBody, "the stored response is replayed without re-running the handler");
        (Recorder.Count - baseline).Should().Be(1);

        var stored = await ReadStoredKeyAsync(organizationId, key, token);
        stored.Should().NotBeNull();
        stored!.ResponseStatus.Should().Be((int)HttpStatusCode.OK);
        stored.RequestHash.Length.Should().Be(32, "the request hash is a SHA-256 digest");
    }

    [Theory]
    [InlineData(CultureCodes.English)]
    [InlineData(CultureCodes.Ukrainian)]
    public async Task SameKeyDifferentBody_Returns409WithStableCode(string language)
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var key = Guid.NewGuid().ToString("N");
        var baseline = Recorder.Count;
        using var client = Client(organizationId, language);

        var first = await PostAsync(client, key, "{\"value\":1}", token);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await PostAsync(client, key, "{\"value\":2}", token);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var document = JsonDocument.Parse(await second.Content.ReadAsStringAsync(token));
        document.RootElement.GetProperty(ApiConstants.ProblemCode).GetString()
            .Should().Be(ErrorCodes.IdempotencyKeyReused);
        document.RootElement.GetProperty("detail").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty(ApiConstants.ProblemTraceId).GetString()
            .Should().NotBeNullOrWhiteSpace();
        (Recorder.Count - baseline).Should().Be(1, "a key reused with another body never runs the handler again");
    }

    [Fact]
    public async Task ConcurrentFirstUse_RunsHandlerOnce()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var key = Guid.NewGuid().ToString("N");
        var baseline = Recorder.Count;
        // Both callers are the same user: the fingerprint includes the user id,
        // so two different users sending one key is a key reuse, not a race.
        var userId = Guid.CreateVersion7();
        using var firstClient = Factory.CreateClientWithTenant(organizationId, userId);
        using var secondClient = Factory.CreateClientWithTenant(organizationId, userId);

        var first = PostAsync(firstClient, key, "{\"value\":1}", token);
        var second = PostAsync(secondClient, key, "{\"value\":1}", token);
        var responses = await Task.WhenAll(first, second);

        responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
        responses[1].StatusCode.Should().Be(HttpStatusCode.OK);
        var bodies = await Task.WhenAll(
            responses.Select(response => response.Content.ReadAsStringAsync(token)));
        bodies[0].Should().Be(bodies[1], "both callers receive the one stored response");
        (Recorder.Count - baseline).Should().Be(1, "concurrent first use of one key runs the handler once");
    }

    [Fact]
    public async Task SameKeyDifferentOrganization_RunsIndependently()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationA = Guid.CreateVersion7();
        var organizationB = Guid.CreateVersion7();
        var key = Guid.NewGuid().ToString("N");
        var baseline = Recorder.Count;
        using var clientA = Client(organizationA);
        using var clientB = Client(organizationB);

        var first = await PostAsync(clientA, key, "{\"value\":1}", token);
        var second = await PostAsync(clientB, key, "{\"value\":1}", token);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (Recorder.Count - baseline).Should().Be(2, "keys are unique per organization, so each organization claims its own");
    }

    [Fact]
    public async Task ThrowingHandler_ReleasesClaim_SoRetryRunsHandlerAgain()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var key = Guid.NewGuid().ToString("N");
        var baseline = Recorder.Count;
        using var client = Client(organizationId);

        Recorder.FailNextExecution();
        var first = await PostAsync(client, key, "{\"value\":1}", token);
        first.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var second = await PostAsync(client, key, "{\"value\":1}", token);

        second.StatusCode.Should().Be(HttpStatusCode.OK,
            "a failed handler releases its claim, so the same key is reusable instead of in-progress until expiry");
        (Recorder.Count - baseline).Should().Be(2, "the handler ran once per attempt");
    }

    [Fact]
    public async Task SameKeyDifferentUser_Returns409WithoutRunningHandler()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var key = Guid.NewGuid().ToString("N");
        var baseline = Recorder.Count;
        using var firstUser = Factory.CreateClientWithTenant(organizationId, Guid.CreateVersion7());
        using var otherUser = Factory.CreateClientWithTenant(organizationId, Guid.CreateVersion7());

        var first = await PostAsync(firstUser, key, "{\"value\":1}", token);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await PostAsync(otherUser, key, "{\"value\":1}", token);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "the fingerprint includes the user, so another user never replays this response");
        (await ProblemCodeAsync(second, token)).Should().Be(ErrorCodes.IdempotencyKeyReused);
        (Recorder.Count - baseline).Should().Be(1);
    }

    [Fact]
    public async Task OversizedBody_Returns400WithStableCodeWithoutRunningHandler()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var baseline = Recorder.Count;
        using var client = Client(organizationId);
        var oversized = new string('x', (int)IdempotencyOptions.DefaultMaxBodyBytes + 1);

        var response = await PostAsync(client, Guid.NewGuid().ToString("N"), oversized, token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "an oversized body is a validation failure, not a conflict");
        (await ProblemCodeAsync(response, token)).Should().Be(ErrorCodes.IdempotencyBodyTooLarge);
        (Recorder.Count - baseline).Should().Be(0);
    }

    [Fact]
    public async Task OverLongKey_Returns400WithStableCode()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        using var client = Client(organizationId);
        var key = new string('k', IdempotencyOptions.MaximumKeyLength + 1);

        var response = await PostAsync(client, key, "{\"value\":1}", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemCodeAsync(response, token)).Should().Be(ErrorCodes.IdempotencyKeyTooLong);
    }

    [Fact]
    public async Task ExpiredClaim_TakeoverRemovesOnlyTheExpiredRow()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var key = Guid.NewGuid().ToString("N");
        var now = fixture.TimeProvider.GetUtcNow().UtcDateTime;
        var hash = new byte[] { 1, 2, 3 };
        var tenantContext = new JobTenantContext();
        tenantContext.Set(organizationId);

        await using var context = TenantTestDbContextFactory.Create(fixture.ConnectionString, tenantContext, fixture.TimeProvider);
        var store = new EfIdempotencyStore<TenantTestDbContext>(context, tenantContext);

        var expiredClaimId = await store.TryClaimAsync(key, hash, now.AddMinutes(-1), token);
        expiredClaimId.Should().NotBeNull();
        (await store.TryDeleteExpiredClaimAsync(expiredClaimId!.Value, now, token)).Should().BeTrue(
            "the expired row is removed by its identity");

        var freshClaimId = await store.TryClaimAsync(key, hash, now.AddDays(1), token);
        freshClaimId.Should().NotBeNull("the key is claimable again after the takeover");

        (await store.TryDeleteExpiredClaimAsync(expiredClaimId.Value, now, token)).Should().BeFalse(
            "a stale caller holding the old identity never removes the fresh claim");
        (await store.TryDeleteExpiredClaimAsync(freshClaimId!.Value, now, token)).Should().BeFalse(
            "a live claim is never removed by the expiry takeover");

        var remaining = await context.Set<IdempotencyKey>().AsNoTracking()
            .SingleAsync(entry => entry.Key == key, token);
        remaining.Id.Should().Be(freshClaimId.Value);
    }

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response, CancellationToken token)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        return document.RootElement.GetProperty(ApiConstants.ProblemCode).GetString();
    }

    private HttpClient Client(Guid organizationId, string? language = null)
    {
        var client = Factory.CreateClientWithTenant(organizationId, Guid.CreateVersion7());
        if (language is not null)
        {
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(language));
        }

        return client;
    }

    [Theory]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status403Forbidden)]
    [InlineData(StatusCodes.Status429TooManyRequests)]
    public async Task StaleAuthOrRateLimitFailure_IsNotStored_SoRetryRunsAgain(int status)
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var key = Guid.NewGuid().ToString("N");
        var baseline = Recorder.Count;
        using var client = Client(organizationId);
        var path = $"/test/idempotency/status/{status}";

        var first = await PostAsync(client, path, key, "{\"value\":1}", token);
        first.StatusCode.Should().Be((HttpStatusCode)status);

        var second = await PostAsync(client, path, key, "{\"value\":1}", token);

        second.StatusCode.Should().Be((HttpStatusCode)status);
        (Recorder.Count - baseline).Should().Be(2,
            "a permission or rate-limit failure is never replayed; the retry runs the handler again");
    }

    [Fact]
    public async Task Replay_KeepsLocationAndEtag_AndBinaryBodyExact()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var key = Guid.NewGuid().ToString("N");
        var baseline = Recorder.Count;
        using var client = Client(organizationId);
        var path = "/test/idempotency/resource";

        var first = await PostAsync(client, path, key, "{\"value\":1}", token);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await PostAsync(client, path, key, "{\"value\":1}", token);

        second.StatusCode.Should().Be(HttpStatusCode.Created, "the stored status is replayed");
        second.Headers.Location!.ToString().Should().Be("/test/idempotency/resource/1",
            "Location is on the replay allow-list");
        second.Headers.GetValues(HeaderNames.ETag).Should().ContainSingle("\"v1\"",
            "ETag is on the replay allow-list");
        (await second.Content.ReadAsByteArrayAsync(token)).Should().Equal(IdempotencyProbeController.BinaryPayload,
            "the replayed body is byte-exact, never decoded as text");
        (Recorder.Count - baseline).Should().Be(1, "the replay never runs the handler again");
    }

    private static Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string key,
        string json,
        CancellationToken token) =>
        PostAsync(client, "/test/idempotency/increment", key, json, token);

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string path,
        string key,
        string json,
        CancellationToken token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Add(ApiConstants.IdempotencyKeyHeader, key);
        return await client.SendAsync(request, token);
    }

    private async Task<IdempotencyKey?> ReadStoredKeyAsync(
        Guid organizationId,
        string key,
        CancellationToken token)
    {
        var tenantContext = new JobTenantContext();
        tenantContext.Set(organizationId);
        await using var context = TenantTestDbContextFactory.Create(fixture.ConnectionString, tenantContext, fixture.TimeProvider);
        return await context.Set<IdempotencyKey>().SingleOrDefaultAsync(entry => entry.Key == key, token);
    }

    private static WebApplicationFactory<Program> BuildFactory(PostgresFixture fixture) =>
        fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Hangfire:Queues:0"] = TestHangfireQueues.IdempotencyHost,
                });
            });
            builder.ConfigureServices(services =>
            {
                services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());
                services.AddSingleton(Recorder);
                services.AddScoped<IdempotencyKeyFilter>();
                services.AddScoped(provider =>
                {
                    var tenant = provider.GetRequiredService<ITenantContext>();
                    return new TenantTestDbContext(
                        TenantTestDbContextFactory.CreateOptions(fixture.ConnectionString, tenant, fixture.TimeProvider),
                        tenant);
                });
                services.AddScoped<IIdempotencyStore, EfIdempotencyStore<TenantTestDbContext>>();
            });
        });
}
