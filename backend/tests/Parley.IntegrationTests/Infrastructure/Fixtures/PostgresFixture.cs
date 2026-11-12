using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Parley.Campaigns.Persistence;
using Parley.Crm.Persistence;
using Parley.Identity.Persistence;
using Parley.Insights.Persistence;
using Parley.IntegrationTests.Access.Support;
using Parley.IntegrationTests.Api.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;
using Parley.IntegrationTests.Tenancy.Support;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Implementations;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(global::Parley.IntegrationTests.Infrastructure.Fixtures.PostgresFixture))]

namespace Parley.IntegrationTests.Infrastructure.Fixtures;

/// <summary>
/// Shared test database: one pgvector Postgres container per run, or the
/// compose Postgres when <see cref="TestDatabaseSettings.EnvironmentVariableName"/>
/// is set. Schema is created once; data is reset between tests with Respawn
/// over the module schemas only.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private static readonly DateTimeOffset StartInstant = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private const string ProbeTableName = "tenant_records";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(TestDatabaseImages.Postgres)
        .WithUsername("parley")
        .WithPassword("parley")
        .WithDatabase("parley")
        .Build();

    private readonly FakeTimeProvider _timeProvider = new(StartInstant);

    private WebApplicationFactory<Program>? _factory;

    private Respawner? _respawner;

    public PostgreSqlContainer Postgres => _postgres;

    public string ConnectionString { get; private set; } = string.Empty;

    public FakeTimeProvider TimeProvider => _timeProvider;

    public WebApplicationFactory<Program> Factory => _factory ?? throw new InvalidOperationException("Fixture is not initialized.");

    public async ValueTask InitializeAsync()
    {
        var external = Environment.GetEnvironmentVariable(TestDatabaseSettings.EnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(external))
        {
            await _postgres.StartAsync();
            ConnectionString = _postgres.GetConnectionString();
        }
        else
        {
            ConnectionString = external;
            await EnsureTestDatabaseExistsAsync(external);
        }

        await using (var designContext = DesignContext(ConnectionString))
        {
            await EnsureSchemaCreatedAsync(designContext, $"{TenantTestDbContext.SchemaName}.{ProbeTableName}");
        }

        await using (var secondContext = SecondDesignContext(ConnectionString))
        {
            await EnsureSchemaCreatedAsync(secondContext, $"{SecondSchemaDbContext.SchemaName}.{ProbeTableName}");
        }

        // Identity schema is created by its migrations, not by the probe above.
        await using (var identityContext = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options,
            new JobTenantContext()))
        {
            await identityContext.Database.MigrateAsync();
        }

        // The CRM schema is created by its own migrations, the same way.
        await using (var crmContext = new CrmDbContext(
            new DbContextOptionsBuilder<CrmDbContext>()
                .UseNpgsql(ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options,
            new JobTenantContext()))
        {
            await crmContext.Database.MigrateAsync();
        }

        // The campaigns schema is created by its own migrations, the same way.
        await using (var campaignsContext = new CampaignsDbContext(
            new DbContextOptionsBuilder<CampaignsDbContext>()
                .UseNpgsql(ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options,
            new JobTenantContext()))
        {
            await campaignsContext.Database.MigrateAsync();
        }

        // The insights schema is created by its own migrations, the same way. Its scoring tables need the vector extension.
        await using (var insightsContext = new InsightsDbContext(
            new DbContextOptionsBuilder<InsightsDbContext>()
                .UseNpgsql(ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options,
            new JobTenantContext()))
        {
            await insightsContext.Database.MigrateAsync();
        }

        // The notifications schema is created by its own migrations, the same way.
        await using (var notificationsContext = new NotificationsDbContext(
            new DbContextOptionsBuilder<NotificationsDbContext>()
                .UseNpgsql(ConnectionString)
                .UseSnakeCaseNamingConvention()
                .Options,
            new JobTenantContext()))
        {
            await notificationsContext.Database.MigrateAsync();
        }

        await RegistrationRollbackTrigger.InstallAsync(ConnectionString, CancellationToken.None);

        await using (var designConnection = new NpgsqlConnection(ConnectionString))
        {
            await designConnection.OpenAsync();
            // Only the test schemas are reset: the hangfire schema and the
            // migration history table live outside them and are never truncated.
            _respawner = await Respawner.CreateAsync(designConnection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = [TenantTestDbContext.SchemaName, SecondSchemaDbContext.SchemaName, IdentityDbContext.SchemaName, CrmDbContext.SchemaName, CampaignsDbContext.SchemaName, InsightsDbContext.SchemaName, NotificationsDbContext.SchemaName],
                SchemasToExclude = [PersistenceConstants.HangfireSchema],
                // Migration history lives in the default (public) schema and must survive resets,
                // or the next host start re-runs Initial.
                TablesToIgnore =
                [
                    new Table(IdentityDbContext.SchemaName, PersistenceConstants.MigrationHistoryTable),
                    new Table(CrmDbContext.SchemaName, PersistenceConstants.MigrationHistoryTable),
                    new Table(CampaignsDbContext.SchemaName, PersistenceConstants.MigrationHistoryTable),
                    new Table(InsightsDbContext.SchemaName, PersistenceConstants.MigrationHistoryTable),
                    new Table(NotificationsDbContext.SchemaName, PersistenceConstants.MigrationHistoryTable),
                    new Table(TestDatabaseSettings.PublicSchema, PersistenceConstants.MigrationHistoryTable),
                    // System roles and their grants are seeded by the migration; a reset would drop them.
                    new Table(IdentityDbContext.SchemaName, IdentityDbContext.RolesTableName),
                    new Table(IdentityDbContext.SchemaName, IdentityDbContext.RolePermissionsTableName),
                ],
            });
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                var settings = TestHostSettings.StartupSettings(TestHostSettings.UnusedSmtpHost, TestHostSettings.UnusedSmtpPort);
                settings["Database:ConnectionString"] = ConnectionString;
                settings["Hangfire:Queues:0"] = TestHangfireQueues.FixtureHost;
                configuration.AddInMemoryCollection(settings);
            });
            builder.ConfigureServices(services =>
            {
                // The API host registers TimeProvider.System in Program; swap in the
                // fake instance so HTTP-level time reads are deterministic too.
                var withoutFakeTime = services
                    .Where(descriptor => descriptor.ServiceType != typeof(TimeProvider))
                    .ToArray();
                services.Clear();
                foreach (var descriptor in withoutFakeTime)
                {
                    services.Add(descriptor);
                }

                services.AddSingleton<TimeProvider>(_timeProvider);
                // Outbox passes are driven explicitly by the tests that assert on them.
                OutboxDelivery.RemoveBackgroundLoops(services);
                services.AddAuthentication(TestAuthDefaults.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthDefaults.AuthenticationScheme, _ => { });
                services.AddAuthorization();
                services.AddControllers().AddApplicationPart(typeof(ThrowingTestController).Assembly);
            });
        });
    }

    public async Task ResetDataAsync(CancellationToken cancellationToken = default)
    {
        if (_respawner is null)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await _respawner.ResetAsync(connection);
    }

    public async ValueTask DisposeAsync()
    {
        _factory?.Dispose();
        await _postgres.DisposeAsync();
    }

    /// <summary>
    /// EF's <c>EnsureCreatedAsync</c> skips creation when ANY table exists in the
    /// database, so the second module schema would never be created after the
    /// first. This creates each module schema from its own create script,
    /// keyed by one probe table in that schema.
    /// </summary>
    private async Task EnsureSchemaCreatedAsync(DbContext context, string probeQualifiedName)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var probe = new NpgsqlCommand("SELECT to_regclass(@p0) IS NOT NULL", connection))
        {
            probe.Parameters.AddWithValue("p0", probeQualifiedName);
            if (await probe.ExecuteScalarAsync() is true)
            {
                return;
            }
        }

        var schema = context.Model.GetDefaultSchema()
            ?? throw new InvalidOperationException("Test DbContext must declare a default schema.");
        var script = $"CREATE SCHEMA IF NOT EXISTS \"{schema}\";" + Environment.NewLine + context.Database.GenerateCreateScript();
        await using var create = new NpgsqlCommand(script, connection);
        await create.ExecuteNonQueryAsync();
    }

    private TenantTestDbContext DesignContext(string connectionString)
    {
        var tenantContext = new JobTenantContext();
        tenantContext.Set(Guid.CreateVersion7());
        return new TenantTestDbContext(
            TenantTestDbContextFactory.CreateOptions(ConnectionString, tenantContext, _timeProvider),
            tenantContext);
    }

    private SecondSchemaDbContext SecondDesignContext(string connectionString)
    {
        var tenantContext = new JobTenantContext();
        tenantContext.Set(Guid.CreateVersion7());
        return SecondSchemaDbContextFactory.Create(ConnectionString, tenantContext, _timeProvider);
    }

    private static async Task EnsureTestDatabaseExistsAsync(string externalConnectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(externalConnectionString);
        var databaseName = builder.Database;
        if (string.IsNullOrWhiteSpace(databaseName)
            || databaseName.Any(character => !char.IsLetterOrDigit(character) && character != '_'))
        {
            throw new InvalidOperationException(
                $"The {TestDatabaseSettings.EnvironmentVariableName} connection string must carry a plain database name.");
        }

        builder.Database = TestDatabaseSettings.MaintenanceDatabase;
        await using var adminConnection = new NpgsqlConnection(builder.ConnectionString);
        await adminConnection.OpenAsync();

        await using (var exists = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @p0", adminConnection))
        {
            exists.Parameters.AddWithValue("p0", databaseName);
            if (await exists.ExecuteScalarAsync() is not null)
            {
                return;
            }
        }

        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", adminConnection);
        await create.ExecuteNonQueryAsync();
    }
}
