using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Companies;

/// <summary>Company views carry the owner name and email from the user directory, on create, list and detail.</summary>
public sealed class CompanyDisplayFieldsTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";
    private const string OwnerDisplayNameProperty = "ownerDisplayName";
    private const string OwnerEmailProperty = "ownerEmail";
    private const string CompanyName = "Acme Ltd";
    private const string OwnerName = "Admin Owner";
    private const int ListLimit = 50;

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
    public async Task CompanyCreateListAndDetail_CarryOwnerNameAndEmail()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, owner.UserId, OwnerName, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        var created = await CreateCompanyAsync(admin, owner.UserId, token);
        var id = CrmApi.IdOf(created);
        AssertOwner(created, owner.Email);

        var detail = await GetJsonAsync(admin, CrmApi.CompanyRoute(id), token);
        AssertOwner(detail, owner.Email);

        var list = await GetJsonAsync(admin, $"{CrmRoutes.Companies}?limit={ListLimit}", token);
        AssertOwner(ItemById(list, id), owner.Email);
    }

    private static void AssertOwner(JsonElement view, string ownerEmail)
    {
        view.GetProperty(OwnerDisplayNameProperty).GetString().Should().Be(OwnerName);
        view.GetProperty(OwnerEmailProperty).GetString().Should().Be(ownerEmail);
    }

    private static JsonElement ItemById(JsonElement page, Guid id) =>
        page.GetProperty(ItemsProperty).EnumerateArray().Single(item => item.GetProperty(IdProperty).GetGuid() == id);

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string route, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(route, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    private static async Task<JsonElement> CreateCompanyAsync(HttpClient client, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(CrmRoutes.Companies, new { name = CompanyName, ownerUserId }, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }
}
