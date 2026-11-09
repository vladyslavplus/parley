using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Crm.Companies;

/// <summary>
/// Companies follow the contact rules: another organization's company is not found for any write, the
/// list follows the role scope, and a write with a stale version is rejected with a stable code.
/// </summary>
public sealed class CompanyTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";
    private const string CompanyName = "Acme Ltd";
    private const int ListLimit = 200;

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
    public async Task CompanyOfAnotherOrganization_IsNotFound_ForReadAndWrites()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var company = await CreateCompanyAsync(admin, new { name = CompanyName }, token);
        var id = CrmApi.IdOf(company);
        var version = CrmApi.VersionOf(company);

        var otherOwner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var other = CrmApi.Client(_factory, otherOwner.AccessToken);

        (await other.GetAsync(CrmApi.CompanyRoute(id), token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await other.PutAsJsonAsync(CrmApi.CompanyRoute(id), new { version, name = "Other" }, token))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await other.DeleteAsync(CrmApi.CompanyRoute(id), token)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await admin.DeleteAsync(CrmApi.CompanyRoute(id), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await other.PostAsync(IdentityApi.Route(CrmRoutes.CompanyRestore, id), null, token))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompanyList_FollowsRoleScope()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var repA = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var repB = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var manager = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesManagerRoleId, token);
        await CrmApi.CreateTeamAsync(_factory, admin, owner.OrganizationId, [repA.UserId, manager.UserId], token);

        var ownedByA = CrmApi.IdOf(await CreateCompanyAsync(admin, new { name = CompanyName, ownerUserId = repA.UserId }, token));
        _ = await CreateCompanyAsync(admin, new { name = CompanyName, ownerUserId = repB.UserId }, token);

        using var repClient = CrmApi.Client(_factory, repA.AccessToken);
        using var managerClient = CrmApi.Client(_factory, manager.AccessToken);
        (await ListIdsAsync(repClient, token)).Should().BeEquivalentTo([ownedByA]);
        (await ListIdsAsync(managerClient, token)).Should().BeEquivalentTo([ownedByA]);
    }

    [Fact]
    public async Task StaleVersion_ReturnsConflict_AfterAnotherWrite()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var created = await CreateCompanyAsync(admin, new { name = CompanyName }, token);
        var route = CrmApi.CompanyRoute(CrmApi.IdOf(created));
        var readVersion = CrmApi.VersionOf(created);

        var first = await admin.PutAsJsonAsync(route, new { version = readVersion, name = "Acme Group" }, token);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var stale = await admin.PutAsJsonAsync(route, new { version = readVersion, name = "Acme Corp" }, token);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(stale, token)).Should().Be(ErrorCodes.ConcurrencyConflict);
    }

    private static async Task<JsonElement> CreateCompanyAsync(HttpClient client, object body, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(CrmRoutes.Companies, body, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    private static async Task<List<Guid>> ListIdsAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{CrmRoutes.Companies}?limit={ListLimit}", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty).EnumerateArray()
            .Select(item => item.GetProperty(IdProperty).GetGuid())
            .ToList();
    }
}
