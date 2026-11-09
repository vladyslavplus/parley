using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api;
using Parley.Api.Controllers.Crm;
using Parley.Api.Controllers.Identity;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Api;

/// <summary>The generated OpenAPI document lists the allowed values of list sort, status and type parameters.</summary>
public sealed class OpenApiListParameterTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string Get = "get";
    private const string Parameters = "parameters";
    private const string NameProperty = "name";
    private const string SchemaProperty = "schema";
    private const string EnumProperty = "enum";
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
    [InlineData("/" + IdentityRoutes.Members, "status", "deactivated")]
    [InlineData("/" + IdentityRoutes.AuditEvents, "sort", "occurredAt")]
    [InlineData("/" + CrmRoutes.CustomFields, "entityType", "contact")]
    public async Task ListParameter_DocumentsItsAllowedValues(string path, string parameter, string value)
    {
        var token = TestContext.Current.CancellationToken;
        using var document = await ReadDocumentAsync(token);

        var allowed = document.RootElement
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty(Get)
            .GetProperty(Parameters)
            .EnumerateArray()
            .Single(item => item.GetProperty(NameProperty).GetString() == parameter)
            .GetProperty(SchemaProperty)
            .GetProperty(EnumProperty)
            .EnumerateArray()
            .Select(item => item.GetString());

        allowed.Should().Contain(value, $"{path}?{parameter} is a union on the generated client");
    }

    private async Task<JsonDocument> ReadDocumentAsync(CancellationToken cancellationToken)
    {
        using var client = AuthTestClient.CreateClient(_factory, CultureCodes.English);
        using var response = await client.GetAsync($"/openapi/{ApiConstants.OpenApiDocumentName}.json", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
