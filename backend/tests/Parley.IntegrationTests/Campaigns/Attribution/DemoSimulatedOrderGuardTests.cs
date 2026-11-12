using AwesomeAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Seeding.DemoSeed;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Attribution;

/// <summary>
/// The guard of the simulated demo orders: the writer refuses every target that is not a demo environment on a demo
/// database, and it writes nothing when it refuses. The test database is never a demo database, so every case must refuse.
/// </summary>
public sealed class DemoSimulatedOrderGuardTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string DemoOrdersCountSql = "SELECT COUNT(*) FROM crm.orders WHERE source = 'demo_simulated'";

    private const string ProductionEnvironment = "Production";

    private const string DevelopmentEnvironment = "Development";

    private const string DemoEnvironment = "Demo";

    private const string TestApplicationName = "Parley.IntegrationTests";

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [InlineData(ProductionEnvironment)]
    [InlineData(DevelopmentEnvironment)]
    [InlineData(DemoEnvironment)]
    public async Task Writer_RefusesEveryTarget_OutsideTheDemoSeed_AndWritesNothing(string environmentName)
    {
        var token = TestContext.Current.CancellationToken;
        await using var dataSource = NpgsqlDataSource.Create(postgres.ConnectionString);
        var writer = new CrmDemoSimulatedOrderWriter(dataSource, new FixedEnvironment(environmentName));
        var order = new CrmSimulatedOrder(Guid.NewGuid(), "demo-sim:guard", DateTimeOffset.UtcNow, 10m);

        var act = () => writer.WriteAsync(Guid.NewGuid(), [order], token);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await CountDemoOrdersAsync(token)).Should().Be(0);
    }

    private async Task<long> CountDemoOrdersAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(DemoOrdersCountSql, connection);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    /// <summary>A host environment with a fixed name, for the guard only.</summary>
    private sealed class FixedEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = TestApplicationName;

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
