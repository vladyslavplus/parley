using System.Net;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Authorization;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Identity.Organizations;

/// <summary>
/// <c>GET /me/organizations</c>: a user sees every organization of an active membership, marks the
/// organization of the current token, and never sees a deactivated membership.
/// </summary>
public sealed class MyOrganizationsTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
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
    public async Task UserInTwoOrganizations_SeesBoth_AndMarksTheCurrentOne()
    {
        var token = TestContext.Current.CancellationToken;
        var home = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var other = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await MembershipTestData.AddMembershipAsync(_factory.Services, other.OrganizationId, home.UserId, SystemRoleSeed.SalesRepRoleId, _factory.Clock, token);

        var signedIn = await IdentityApi.SignInAsync(_factory, home.Email, token);
        using var client = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, signedIn.AccessToken);
        using var response = await client.GetAsync(IdentityRoutes.MeOrganizations, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var organizations = (await AuthTestClient.ReadJsonAsync(response, token)).EnumerateArray().ToList();
        organizations.Should().HaveCount(2, "both active memberships are listed");

        var current = organizations.Single(entry => entry.GetProperty("id").GetGuid() == home.OrganizationId);
        current.GetProperty("roleCode").GetString().Should().Be(SystemRoles.Admin);
        current.GetProperty("isCurrent").GetBoolean().Should().BeTrue();

        var joined = organizations.Single(entry => entry.GetProperty("id").GetGuid() == other.OrganizationId);
        joined.GetProperty("roleCode").GetString().Should().Be(SystemRoles.SalesRep);
        joined.GetProperty("isCurrent").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task DeactivatedMembership_IsLeftOut()
    {
        var token = TestContext.Current.CancellationToken;
        var home = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var other = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await MembershipTestData.AddMembershipAsync(_factory.Services, other.OrganizationId, home.UserId, SystemRoleSeed.SalesRepRoleId, _factory.Clock, token);

        var membershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, other.OrganizationId, home.UserId, token);
        using (var otherAdmin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, other.AccessToken))
        {
            using var deactivated = await otherAdmin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, membershipId), null, token);
            deactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var signedIn = await IdentityApi.SignInAsync(_factory, home.Email, token);
        using var client = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, signedIn.AccessToken);
        using var response = await client.GetAsync(IdentityRoutes.MeOrganizations, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var organizations = (await AuthTestClient.ReadJsonAsync(response, token)).EnumerateArray().ToList();
        organizations.Select(entry => entry.GetProperty("id").GetGuid()).Should().BeEquivalentTo([home.OrganizationId],
            "the deactivated membership in the other organization is not listed");
    }
}
