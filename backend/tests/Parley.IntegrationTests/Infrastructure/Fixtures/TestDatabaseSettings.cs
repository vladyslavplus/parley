namespace Parley.IntegrationTests.Infrastructure.Fixtures;

/// <summary>
/// Where integration tests run. Default is a Testcontainers PostgreSQL
/// instance; set the environment variable to the compose Postgres instead,
/// for example with a separate test database.
/// </summary>
public static class TestDatabaseSettings
{
    /// <summary>
    /// Name of the environment variable holding a full Npgsql connection
    /// string for the compose Postgres, for example
    /// "Host=127.0.0.1;Port=5433;Database=parley_test;Username=parley;Password=parley".
    /// Unset (the default) starts a Testcontainers Postgres container.
    /// </summary>
    public const string EnvironmentVariableName = "PARLEY_TEST_POSTGRES";

    /// <summary>Compose Postgres host used when building the external connection string.</summary>
    public const string ExternalHost = "127.0.0.1";

    /// <summary>Compose Postgres port used when building the external connection string.</summary>
    public const int ExternalPort = 5433;

    /// <summary>Database name used for integration tests on the external Postgres.</summary>
    public const string TestDatabaseName = "parley_test";

    /// <summary>Default schema of the test database, where EF keeps the migration history.</summary>
    public const string PublicSchema = "public";

    /// <summary>Maintenance database used to create the test database when running externally.</summary>
    public const string MaintenanceDatabase = "postgres";
}
