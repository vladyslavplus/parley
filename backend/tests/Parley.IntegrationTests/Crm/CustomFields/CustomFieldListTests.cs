using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Infrastructure.Paging;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Crm.CustomFields;

/// <summary>The custom field list is keyset-paged, searches labels and keys, filters by type and sorts by name or position.</summary>
public sealed class CustomFieldListTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string LabelProperty = "label";
    private const string KeyProperty = "key";
    private const string FieldTypeNumber = "number";
    private const string FieldTypeText = "text";
    private const string EntityContact = "contact";
    private const string EntityCompany = "company";
    private const string UnknownEntityType = "invoice";

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
    public async Task CustomFieldList_SearchesFiltersSortsAndPages()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        await CreateDefinitionAsync(admin, "lead_score", "Lead score", FieldTypeNumber, position: 2, token);
        await CreateDefinitionAsync(admin, "budget", "Budget", FieldTypeNumber, position: 0, token);
        await CreateDefinitionAsync(admin, "notes", "Notes", FieldTypeText, position: 1, token);

        (await PropertyAsync(admin, $"{CrmRoutes.CustomFields}", LabelProperty, token))
            .Should().Equal("Budget", "Lead score", "Notes");
        (await PropertyAsync(admin, $"{CrmRoutes.CustomFields}?sort=position&order=asc", KeyProperty, token))
            .Should().Equal("budget", "notes", "lead_score");
        (await PropertyAsync(admin, $"{CrmRoutes.CustomFields}?fieldType={FieldTypeText}", LabelProperty, token))
            .Should().Equal("Notes");
        (await PropertyAsync(admin, $"{CrmRoutes.CustomFields}?q=score", LabelProperty, token))
            .Should().Equal("Lead score");
        (await PropertyAsync(admin, $"{CrmRoutes.CustomFields}?entityType={EntityCompany}", LabelProperty, token))
            .Should().BeEmpty();
        (await ListPages.ReadAllAsync(admin, $"{CrmRoutes.CustomFields}?sort=name&order=desc&limit=1", token))
            .Select(row => row.GetProperty(LabelProperty).GetString())
            .Should().Equal("Notes", "Lead score", "Budget");
    }

    [Fact]
    public async Task CustomFieldList_RejectsAnUnknownEntityType_WithValidationCode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        var response = await admin.GetAsync($"{CrmRoutes.CustomFields}?entityType={UnknownEntityType}", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(response, "entityType", token)).Should().Be(ErrorCodes.ValidationFailed);
    }

    private static async Task CreateDefinitionAsync(HttpClient admin, string key, string label, string fieldType, int position, CancellationToken token)
    {
        var response = await admin.PostAsJsonAsync(
            CrmRoutes.CustomFields,
            new { entityType = EntityContact, key, label, fieldType, options = (IReadOnlyList<string>?)null, isRequired = false, position },
            token);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<List<string?>> PropertyAsync(HttpClient client, string route, string property, CancellationToken token)
    {
        var response = await client.GetAsync(route, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, token);
        return page.GetProperty("items").EnumerateArray().Select(row => row.GetProperty(property).GetString()).ToList();
    }
}
