using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>
/// Record visibility follows the role: own for a sales rep, team for a sales manager, organization
/// for a marketer. Read-only roles cannot write, and out-of-scope records are not found.
/// </summary>
public sealed class ContactScopeTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ItemsProperty = "items";

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
    public async Task ListAndWrites_FollowRoleScope()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var repA = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var repB = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var manager = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesManagerRoleId, token);
        var marketer = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.MarketerRoleId, token);
        var viewer = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.ViewerRoleId, token);
        await CrmApi.CreateTeamAsync(_factory, admin, owner.OrganizationId, [repA.UserId, manager.UserId], token);

        var ownedByA = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repA.UserId), token));
        var ownedByB = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repB.UserId), token));

        using var repAClient = CrmApi.Client(_factory, repA.AccessToken);
        using var managerClient = CrmApi.Client(_factory, manager.AccessToken);
        using var marketerClient = CrmApi.Client(_factory, marketer.AccessToken);
        using var viewerClient = CrmApi.Client(_factory, viewer.AccessToken);

        (await ListIdsAsync(repAClient, token)).Should().BeEquivalentTo([ownedByA]);
        (await ListIdsAsync(managerClient, token)).Should().BeEquivalentTo([ownedByA]);
        (await ListIdsAsync(marketerClient, token)).Should().BeEquivalentTo([ownedByA, ownedByB]);
        (await ListIdsAsync(viewerClient, token)).Should().BeEmpty();

        (await viewerClient.PostAsJsonAsync(CrmRoutes.Contacts, ContactFaker.Create(), token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await repAClient.DeleteAsync(CrmApi.ContactRoute(ownedByA), token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await managerClient.DeleteAsync(CrmApi.ContactRoute(ownedByB), token))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<List<Guid>> ListIdsAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{CrmRoutes.Contacts}?limit=200", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty).EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToList();
    }

}
