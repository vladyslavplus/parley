using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Errors;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.CustomFields;

/// <summary>A custom field value is checked against its definition; a bad value returns a field error with a stable code.</summary>
public sealed class CustomFieldValidationTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string FieldKey = "lead_score";
    private const string FieldPathPrefix = "customFields.";
    private const string FieldCodeProperty = "code";
    private const int ValidScore = 5;

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

    [Fact]
    public async Task WrongType_ReturnsFieldError_AndRightType_IsStored()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var definition = await admin.PostAsJsonAsync(
            CrmRoutes.CustomFields,
            new { entityType = "contact", key = FieldKey, label = "Lead score", fieldType = "number", options = (IReadOnlyList<string>?)null, isRequired = false, position = 0 },
            token);
        definition.StatusCode.Should().Be(HttpStatusCode.Created);

        var wrongType = ContactFaker.Create() with
        {
            CustomFields = new Dictionary<string, JsonElement> { [FieldKey] = JsonSerializer.SerializeToElement("high") },
        };
        var rejected = await admin.PostAsJsonAsync(CrmRoutes.Contacts, wrongType, token);

        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await AuthTestClient.ReadJsonAsync(rejected, token);
        var fieldCode = problem.GetProperty(ApiConstants.ProblemErrors)
            .GetProperty(FieldPathPrefix + FieldKey)[0]
            .GetProperty(FieldCodeProperty)
            .GetString();
        fieldCode.Should().Be(CrmErrorCodes.CustomFieldTypeMismatch);

        var rightType = wrongType with
        {
            CustomFields = new Dictionary<string, JsonElement> { [FieldKey] = JsonSerializer.SerializeToElement(ValidScore) },
        };
        var accepted = await CrmApi.CreateContactAsync(admin, rightType, token);
        accepted.GetProperty("customFields").GetProperty(FieldKey).GetInt32().Should().Be(ValidScore);
    }
}
