using System.Net;
using AwesomeAssertions;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Xunit;

namespace Parley.IntegrationTests.Api;

public sealed class HealthEndpointTests(PostgresFixture fixture)
{
    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyWhenDatabaseIsAvailable()
    {
        using var client = fixture.Factory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/health", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(token);
        body.Should().Contain("database");
    }

    [Fact]
    public async Task OpenApiDocument_ReturnsDocumentInDevelopment()
    {
        using var client = fixture.Factory.CreateClient();
        var token = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/openapi/v1.json", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(token);
        body.Should().Contain("\"openapi\"");
    }
}
