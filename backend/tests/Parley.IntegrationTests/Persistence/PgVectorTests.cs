using AwesomeAssertions;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Xunit;

namespace Parley.IntegrationTests.Persistence;

public sealed class PgVectorTests(PostgresFixture fixture)
{
    [Fact]
    public async Task PgVectorExtension_CanBeCreated()
    {
        var token = TestContext.Current.CancellationToken;
        await using var dataSource = Npgsql.NpgsqlDataSource.Create(fixture.ConnectionString);
        await using var command = dataSource.CreateCommand("CREATE EXTENSION IF NOT EXISTS vector;");
        await command.ExecuteNonQueryAsync(token);

        await using var check = dataSource.CreateCommand("SELECT 1 FROM pg_extension WHERE extname = 'vector';");
        var result = await check.ExecuteScalarAsync(token);
        result.Should().NotBeNull();
    }
}
