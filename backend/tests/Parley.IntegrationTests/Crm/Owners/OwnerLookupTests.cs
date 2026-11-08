using System.Net;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Owners;

/// <summary>
/// The owner lookup serves a user who may only read contacts: an administrator sees active members of the
/// organization, a sales representative sees only the owners their scope covers, and deactivated members and
/// other organizations never appear.
/// </summary>
public sealed class OwnerLookupTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string Term = "rep";
    private const string RepName = "Rep Iryna";
    private const string DeactivatedName = "Rep Deactivated";
    private const string OtherOrganizationName = "Rep Other Organization";
    private const string IdProperty = "id";
    private const string DisplayNameProperty = "displayName";
    private const string IsActiveProperty = "isActive";

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
    public async Task OwnerLookup_IsScopedToTheCallersContactAccess_AndSkipsDeactivatedMembers()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, rep.UserId, RepName, token);
        var deactivated = await IdentitySetup.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, deactivated.UserId, DeactivatedName, token);
        var membershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, deactivated.UserId, token);
        (await admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, membershipId), null, token))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var otherOrganization = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, otherOrganization.UserId, OtherOrganizationName, token);

        var adminNames = await OwnerNamesAsync(admin, $"{CrmRoutes.Owners}?q={Term}", token);
        adminNames.Should().Equal(RepName);

        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        var repView = await OwnersAsync(repClient, $"{CrmRoutes.Owners}?q={Term}", token);
        repView.Should().ContainSingle();
        repView[0].GetProperty(IdProperty).GetGuid().Should().Be(rep.UserId);
        repView[0].GetProperty(IsActiveProperty).GetBoolean().Should().BeTrue();

        (await repClient.GetAsync(IdentityRoutes.Members, token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "members stays behind users.manage; owners is the lookup for contact access");
    }

    private static async Task<List<string?>> OwnerNamesAsync(HttpClient client, string route, CancellationToken token) =>
        (await OwnersAsync(client, route, token))
        .Select(owner => owner.GetProperty(DisplayNameProperty).GetString())
        .ToList();

    private static async Task<List<System.Text.Json.JsonElement>> OwnersAsync(HttpClient client, string route, CancellationToken token)
    {
        var response = await client.GetAsync(route, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await AuthTestClient.ReadJsonAsync(response, token);
        return body.EnumerateArray().ToList();
    }
}
