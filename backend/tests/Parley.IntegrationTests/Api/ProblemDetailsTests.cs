using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Identity.Contracts.Errors;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Api;

/// <summary>
/// Every failure shape is the shared ProblemDetails contract (<c>code</c>, localized
/// <c>detail</c>, <c>traceId</c>, field <c>errors</c>) in both supported languages.
/// Validation and not-found come from real authentication endpoints; the unexpected
/// exception comes from a test-only controller.
/// </summary>
public sealed class ProblemDetailsTests(PostgresFixture fixture)
{
    private const string ErrorsProperty = "errors";
    private const string EmailField = "Email";
    private const string FieldCodeProperty = "code";
    private const string FieldMessageProperty = "message";
    private const string CodeProperty = "code";
    private const string DetailProperty = "detail";
    private const string TraceIdProperty = "traceId";

    [Theory]
    [InlineData(CultureCodes.English)]
    [InlineData(CultureCodes.Ukrainian)]
    public async Task Validation_Returns400WithFieldErrors(string language)
    {
        using var client = Client(language);
        var token = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync(
            AuthTestClient.Route(Parley.Api.Controllers.Auth.AuthRoutes.Register),
            new { email = "not-an-address", password = "x", organizationName = "Org" },
            token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var document = await ReadJsonAsync(response, token);
        document.RootElement.GetProperty(CodeProperty).GetString().Should().Be(ErrorCodes.ValidationFailed);
        document.RootElement.GetProperty(DetailProperty).GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty(TraceIdProperty).GetString().Should().NotBeNullOrWhiteSpace();

        var emailError = document.RootElement.GetProperty(ErrorsProperty).GetProperty(EmailField)[0];
        emailError.GetProperty(FieldCodeProperty).GetString().Should().Be(ErrorCodes.ValidationFailed);
        emailError.GetProperty(FieldMessageProperty).GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(CultureCodes.English)]
    [InlineData(CultureCodes.Ukrainian)]
    public async Task NotFound_Returns404WithUserNotFoundCode(string language)
    {
        using var client = Client(language);
        var token = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync(
            AuthTestClient.Route(Parley.Api.Controllers.Auth.AuthRoutes.ConfirmEmail),
            new { userId = Guid.CreateVersion7(), token = "unknown-token" },
            token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var document = await ReadJsonAsync(response, token);
        document.RootElement.GetProperty(CodeProperty).GetString().Should().Be(IdentityErrorCodes.UserNotFound);
        document.RootElement.GetProperty(DetailProperty).GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty(TraceIdProperty).GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(CultureCodes.English)]
    [InlineData(CultureCodes.Ukrainian)]
    public async Task UnexpectedException_Returns500WithoutInternals(string language)
    {
        using var client = Client(language);
        var token = TestContext.Current.CancellationToken;

        var response = await client.GetAsync(TestOnlyRoutes.Throw, token);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync(token);
        body.Should().NotContain(TestOnlyRoutes.ThrowMessage);
        body.Should().NotContain(nameof(InvalidOperationException));
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty(CodeProperty).GetString().Should().Be(ErrorCodes.Unexpected);
        document.RootElement.GetProperty(DetailProperty).GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty(TraceIdProperty).GetString().Should().NotBeNullOrWhiteSpace();
    }

    private HttpClient Client(string language)
    {
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(language));
        return client;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(body);
    }
}
