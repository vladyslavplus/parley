using System.Net;
using AwesomeAssertions;
using Hangfire;
using Hangfire.States;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Parley.Api;
using Parley.IntegrationTests.Hangfire.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;
using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Idempotency;
using Parley.SharedKernel.Idempotency.Persistence;
using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Implementations;
using Xunit;

namespace Parley.IntegrationTests.Hangfire;

/// <summary>
/// Proves the Hangfire dashboard authorization (platform operators only), a
/// tenant-aware job executing under its organization through the worker, a
/// tenant-less job failing loudly, the rerunnable autovacuum post-init SQL,
/// and the idempotency cleanup job against a real Postgres database.
///
/// One factory (and host) serves the whole class; a unique queue keeps this
/// host's Hangfire server away from other hosts' jobs.
/// </summary>
public sealed class HangfireTests(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly ProbeRecorder _recorder = new();

    private WebApplicationFactory<Program>? _factory;

    private WebApplicationFactory<Program> Factory => _factory ??= BuildFactory(fixture);

    public async ValueTask InitializeAsync() =>
        await fixture.ResetDataAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync()
    {
        // The host owns a Hangfire server and an outbox loop on the shared database; stop them with the test.
        _factory?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Dashboard_Anonymous_IsDenied()
    {
        using var client = Factory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var response = await client.GetAsync(ApiConstants.HangfireDashboardRoute, token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "anonymous callers must not open the dashboard");
    }

    [Fact]
    public async Task Dashboard_AuthenticatedNonOperator_IsDenied()
    {
        var organizationId = Guid.CreateVersion7();
        using var client = Factory.CreateClientWithTenant(organizationId, Guid.CreateVersion7());
        var token = TestContext.Current.CancellationToken;

        var response = await client.GetAsync(ApiConstants.HangfireDashboardRoute, token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "organization members are tenants; only platform operators see instance-wide jobs");
    }

    [Fact]
    public async Task Dashboard_PlatformOperator_IsAllowed()
    {
        var organizationId = Guid.CreateVersion7();
        using var client = Factory.CreateClientWithTenant(organizationId, Guid.CreateVersion7(), platformOperator: true);
        var token = TestContext.Current.CancellationToken;

        var response = await client.GetAsync(ApiConstants.HangfireDashboardRoute, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a platform operator passes the dashboard authorization filter");
    }

    [Fact]
    public async Task EnqueuedJob_RunsUnderItsOrganization()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();

        var jobId = Enqueue<TenantProbeJob>(job => job.ExecuteAsync(organizationId, CancellationToken.None));
        await WaitForAsync(
            () => _recorder.SeenOrganizationIds.Contains(organizationId),
            token,
            "the background worker executed the job");
        _recorder.SeenOrganizationIds.Should().ContainSingle(
            seen => seen == organizationId);

        var state = LatestStateName(jobId);
        state.Should().Be(SucceededState.StateName);
    }

    [Fact]
    public async Task EnqueuedJobWithoutOrganization_FailsLoudly()
    {
        var token = TestContext.Current.CancellationToken;

        var jobId = Enqueue<TenantProbeJob>(job => job.ExecuteAsync(null, CancellationToken.None));
        await WaitForAsync(
            () => LatestStateName(jobId) == DeletedState.StateName,
            token,
            "a tenant job without an organization fails and is deleted after its attempts",
            timeout: TimeSpan.FromSeconds(90));

        var state = LatestStateName(jobId);
        state.Should().Be(DeletedState.StateName, "failed jobs do not live forever");
    }

    [Fact]
    public async Task PostInitAutovacuumSql_RunsTwiceWithoutError()
    {
        var token = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(token);

        foreach (var statement in HangfirePostInitSql.AutovacuumStatements)
        {
            await using var command = new NpgsqlCommand(statement, connection);
            await command.ExecuteNonQueryAsync(token);
            await using var repeat = new NpgsqlCommand(statement, connection);
            await repeat.ExecuteNonQueryAsync(token);
        }

        await using var check = new NpgsqlCommand(
            "SELECT reloptions FROM pg_class WHERE oid = @p0::regclass",
            connection);
        check.Parameters.AddWithValue("p0", $"\"{PersistenceConstants.HangfireSchema}\".\"job\"");
        var reloptions = await check.ExecuteScalarAsync(token);
        string.Join(";", (string[])reloptions!).Should().Contain("autovacuum_vacuum_scale_factor=0.05");
    }

    [Fact]
    public async Task CleanupJob_DeletesExpiredKeysOnly()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var tenantContext = new JobTenantContext();
        tenantContext.Set(organizationId);
        var now = fixture.TimeProvider.GetUtcNow().UtcDateTime;

        await using (var context = TenantTestDbContextFactory.Create(fixture.ConnectionString, tenantContext, fixture.TimeProvider))
        {
            context.Set<IdempotencyKey>().Add(new IdempotencyKey(
                organizationId, "expired", [1, 2, 3], DateTime.SpecifyKind(now.AddHours(-1), DateTimeKind.Utc)));
            context.Set<IdempotencyKey>().Add(new IdempotencyKey(
                organizationId, "live", [1, 2, 3], DateTime.SpecifyKind(now.AddHours(1), DateTimeKind.Utc)));
            await context.SaveChangesAsync(token);
        }

        var cleanup = new IdempotencyCleanupJob(
            NpgsqlDataSource.Create(fixture.ConnectionString),
            Options.Create(new IdempotencyOptions { CleanupSchemas = [TenantTestDbContext.SchemaName] }),
            fixture.TimeProvider,
            new JobTenantContext());
        await cleanup.ExecuteAsync(null, token);

        await using var verification = TenantTestDbContextFactory.Create(fixture.ConnectionString, tenantContext, fixture.TimeProvider);
        var remaining = await verification.Set<IdempotencyKey>().ToArrayAsync(token);
        remaining.Select(entry => entry.Key).Should().BeEquivalentTo(["live"]);
    }

    [Fact]
    public async Task TenantJobAndOutboxPass_SaveAndReadTenantRecords_InRealHost()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();

        var jobId = Enqueue<TenantRecordJob>(job => job.ExecuteAsync(organizationId, CancellationToken.None));
        await WaitForAsync(
            () => LatestStateName(jobId) == SucceededState.StateName,
            token,
            "the tenant job saved its record and succeeded");

        using (var scope = Factory.Services.CreateScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
            tenant.Set(organizationId);
            var db = scope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
            scope.ServiceProvider.GetRequiredService<IModuleOutbox<TenantTestDbContext>>().Add(new TestOrderPlaced
            {
                EventId = Guid.CreateVersion7(),
                OccurredAt = fixture.TimeProvider.GetUtcNow(),
                OrderId = orderId,
                Note = "real host",
            });
            await db.SaveChangesAsync(token);
            tenant.Clear();
        }

        var dispatcher = OutboxDelivery.CreateDispatcher<TenantTestDbContext>(Factory.Services);
        var handled = await dispatcher.ProcessDueMessagesAsync(token);

        handled.Should().Be(1, "the dispatcher pass delivers the committed outbox row");
        Factory.Services.GetRequiredService<DeliveryLedger>().DeliveredOrderIds.Should().Contain(orderId);

        using var readScope = Factory.Services.CreateScope();
        var readTenant = readScope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        readTenant.Set(organizationId);
        var reader = readScope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
        var names = await reader.TenantRecords.Select(entry => entry.Name).ToListAsync(token);
        names.Should().BeEquivalentTo(
            [TenantRecordJob.RecordName, $"order-{orderId:D}"],
            "the job and the dispatcher pass each saved a row under the organization, and both read back");
        readTenant.Clear();
    }

    private string Enqueue<TJob>(System.Linq.Expressions.Expression<Func<TJob, Task>> expression)
    {
        using var scope = Factory.Services.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
        return client.Enqueue(expression);
    }

    private static string? LatestStateName(string jobId)
    {
        var details = JobStorage.Current.GetMonitoringApi().JobDetails(jobId);
        return details is { History: { Count: > 0 } } ? details.History[0].StateName : null;
    }

    private static async Task WaitForAsync(
        Func<bool> condition,
        CancellationToken token,
        string reason,
        TimeSpan? timeout = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var limit = timeout ?? TimeSpan.FromSeconds(30);
        while (!condition())
        {
            if (stopwatch.Elapsed > limit)
            {
                throw new TimeoutException(reason);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), token);
        }
    }

    private WebApplicationFactory<Program> BuildFactory(PostgresFixture fixture) =>
        fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Hangfire:Queues:0"] = TestHangfireQueues.HangfireHost,
                    ["Hangfire:QueuePollInterval"] = "00:00:00.100",
                    ["Hangfire:RetryAttempts"] = "1",
                });
            });
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(_recorder);
                services.AddScoped<TenantProbeJob>();
                services.AddScoped<TenantRecordJob>();
                services.AddSingleton<DeliveryLedger>();
                services.AddScoped(provider =>
                {
                    var tenant = provider.GetRequiredService<ITenantContext>();
                    return new TenantTestDbContext(
                        TenantTestDbContextFactory.CreateOptions(fixture.ConnectionString, tenant, fixture.TimeProvider),
                        tenant);
                });
                services.AddOutbox<TenantTestDbContext>(new ConfigurationBuilder().Build());
                services.AddIntegrationEventConsumer<TestOrderPlaced, TenantTestDbContext, RecordingOrderConsumer>();
                // A background loop here would wake whenever another test advances the shared clock
                // and deliver rows that test is asserting on; the test drives its pass explicitly.
                OutboxDelivery.RemoveBackgroundLoops(services);
            });
        });
}
