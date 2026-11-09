using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api;
using Parley.Api.Controllers.Identity;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Api;

/// <summary>
/// The generated OpenAPI document describes success bodies of the identity endpoints, so clients
/// generate typed responses instead of hand-written models.
/// </summary>
public sealed class OpenApiResponseSchemaTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string Ok = "200";

    private const string JsonMediaType = "application/json";

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

    [Theory]
    [InlineData("/" + IdentityRoutes.Me)]
    [InlineData("/" + IdentityRoutes.Members)]
    [InlineData("/" + IdentityRoutes.Teams)]
    [InlineData("/" + IdentityRoutes.AuditEvents)]
    [InlineData("/organizations/{id}")]
    public async Task GetOperation_DocumentsJsonSuccessSchema(string path)
    {
        var token = TestContext.Current.CancellationToken;
        using var document = await ReadDocumentAsync(token);

        var schema = document.RootElement
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty(Ok)
            .GetProperty("content")
            .GetProperty(JsonMediaType)
            .GetProperty("schema");

        schema.ValueKind.Should().NotBe(JsonValueKind.Undefined, $"{path} documents its success body");
    }

    [Theory]
    [InlineData("ContactExportView", "status", "completed")]
    [InlineData("SearchResultItem", "kind", "contact")]
    [InlineData("CustomFieldDefinitionView", "fieldType", "select")]
    [InlineData("CustomFieldDefinitionView", "entityType", "company")]
    public async Task StringValuedProperty_DocumentsItsAllowedValues(string schemaName, string property, string value)
    {
        var token = TestContext.Current.CancellationToken;
        using var document = await ReadDocumentAsync(token);

        var allowed = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty(schemaName)
            .GetProperty("properties")
            .GetProperty(property)
            .GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString());

        allowed.Should().Contain(value, $"{schemaName}.{property} is a union on the generated client");
    }

    private async Task<JsonDocument> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        using var client = AuthTestClient.CreateClient(_factory, CultureCodes.English);
        using var response = await client.GetAsync($"/openapi/{ApiConstants.OpenApiDocumentName}.json", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
