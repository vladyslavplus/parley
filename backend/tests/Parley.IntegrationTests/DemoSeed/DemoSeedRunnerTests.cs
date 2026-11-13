using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Parley.Api.Seeding.Demo;
using Parley.Crm.Contracts.DemoSeed;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.DemoSeed;

/// <summary>
/// The Development demo seed through its runner: a fresh database gets the workspace, a second start is fresh and writes
/// nothing, a stale marker regenerates it, and a non-Development host refuses. The profile is small so the suite stays fast.
/// </summary>
public sealed class DemoSeedRunnerTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string AccountPassword = "test1234";

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(
            postgres,
            mailpit,
            extraConfiguration: new Dictionary<string, string?>
            {
                // The demo test never calls a scoring service; an empty key makes the insights step skip ML immediately.
                ["Insights:Scoring:ApiKey"] = string.Empty,
            });
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AFreshDatabase_GetsTheWorkspace_AndASecondStartIsFresh()
    {
        var first = await Runner(SmallOptions()).RunAsync(TestContext.Current.CancellationToken);

        first.Status.Should().Be(DemoSeedOutcome.Seeded);
        first.Crm!.ContactsInserted.Should().Be(SmallOptions().Profile.Contacts);
        first.Crm.OrdersInserted.Should().BeGreaterThan(0);
        first.Insights!.RfmBuilt.Should().BeTrue();
        first.Insights.PriorityBuilt.Should().BeTrue();
        first.Insights.MlEnabled.Should().BeFalse("the test host has no scoring service key");
        (await CountAsync("SELECT COUNT(*) FROM crm.contacts WHERE organization_id = @org", DemoSeedOrganization.Id))
            .Should().Be(SmallOptions().Profile.Contacts);
        (await CountAsync("SELECT COUNT(*) FROM insights.priority_items WHERE organization_id = @org", DemoSeedOrganization.Id))
            .Should().BeGreaterThan(0);
        (await CountAsync("SELECT COUNT(*) FROM notifications.notifications WHERE organization_id = @org", DemoSeedOrganization.Id))
            .Should().BeGreaterThan(0);

        var second = await Runner(SmallOptions()).RunAsync(TestContext.Current.CancellationToken);

        second.Status.Should().Be(DemoSeedOutcome.Fresh);
    }

    [Fact]
    public async Task AStaleMarker_RegeneratesTheWorkspace_WithDatesRelativeToTheNewClock()
    {
        var first = await Runner(SmallOptions()).RunAsync(TestContext.Current.CancellationToken);
        first.Status.Should().Be(DemoSeedOutcome.Seeded);
        var firstLatest = await LatestOrderAsync();

        _factory.Clock.Advance(TimeSpan.FromDays(10));
        var second = await Runner(SmallOptions()).RunAsync(TestContext.Current.CancellationToken);

        second.Status.Should().Be(DemoSeedOutcome.Regenerated);
        (await LatestOrderAsync()).Should().BeAfter(firstLatest.AddDays(9), "the regenerated orders follow the new clock");
    }

    [Fact]
    public async Task ANonDevelopmentHost_RefusesEvenWithTheSwitchOn()
    {
        var environment = new FixedEnvironment("Production");
        var runner = new DemoSeedRunner(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            environment,
            Options.Create(SmallOptions()),
            _factory.Clock,
            _factory.Services.GetRequiredService<ILogger<DemoSeedRunner>>());

        var outcome = await runner.RunAsync(TestContext.Current.CancellationToken);

        outcome.Status.Should().Be(DemoSeedOutcome.NotDevelopment);
        (await CountAsync("SELECT COUNT(*) FROM crm.contacts WHERE organization_id = @org", DemoSeedOrganization.Id)).Should().Be(0);
    }

    private DemoSeedRunner Runner(DemoSeedOptions options) => new(
        _factory.Services.GetRequiredService<IServiceScopeFactory>(),
        _factory.Services.GetRequiredService<IHostEnvironment>(),
        Options.Create(options),
        _factory.Clock,
        new FileLogger<DemoSeedRunner>(Path.Combine(Path.GetTempPath(), "parley-demo-seed.log")));

    private static DemoSeedOptions SmallOptions() => new()
    {
        Enabled = true,
        RefreshAfterDays = 7,
        AccountPassword = AccountPassword,
        RandomSeed = 20261112,
        Profile = new CrmDemoProfile
        {
            Contacts = 80,
            OrderMonths = 6,
            DealSharePercent = 40,
            TaskSharePercent = 30,
            NoteSharePercent = 15,
            TagSharePercent = 30,
            NoOrdersPercent = 5,
        },
    };

    private async Task<DateTime> LatestOrderAsync()
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT MAX(ordered_at) FROM crm.orders WHERE organization_id = @org", connection);
        command.Parameters.AddWithValue("org", DemoSeedOrganization.Id);
        return (DateTime)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    private async Task<long> CountAsync(string sql, Guid organizationId)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("org", organizationId);
        return (long)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
    }

    /// <summary>Writes the runner's progress to a temp file, so a hanging step is visible in the test output.</summary>
    private sealed class FileLogger<T>(string path) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            File.AppendAllText(path, formatter(state, exception) + Environment.NewLine);
        }
    }

    /// <summary>A host environment with a fixed name, for the refusal test.</summary>
    private sealed class FixedEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Parley.IntegrationTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
