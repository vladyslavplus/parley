using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Contracts.Errors;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Identity.Organizations;

/// <summary>
/// Organization switching: the reissued token carries the target organization, a
/// non-member organization is not found, and data of one organization is not visible in another.
/// </summary>
public sealed class OrganizationSwitchTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
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
    public async Task Switch_ReissuesTokenPair_ForTheTargetOrganization()
    {
        var token = TestContext.Current.CancellationToken;
        var home = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var other = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await MembershipTestData.AddMembershipAsync(_factory.Services, other.OrganizationId, home.UserId, SystemRoleSeed.SalesRepRoleId, _factory.Clock, token);

        var signedIn = await IdentityApi.SignInAsync(_factory, home.Email, token);
        IdentityApi.ReadOrganizationClaim(signedIn.AccessToken).Should().Be(home.OrganizationId.ToString("D"));

        using var client = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, signedIn.AccessToken);
        var switched = await SwitchAsync(client, other.OrganizationId, signedIn.RefreshToken, token);
        switched.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await AuthTestClient.ReadTokensAsync(switched, token);
        IdentityApi.ReadOrganizationClaim(tokens.AccessToken).Should().Be(other.OrganizationId.ToString("D"));

        using var switchedClient = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, tokens.AccessToken);
        var me = await AuthTestClient.ReadJsonAsync(await switchedClient.GetAsync(IdentityRoutes.Me, token), token);
        me.GetProperty("organization").GetProperty("id").GetGuid().Should().Be(other.OrganizationId);
        me.GetProperty("roleCode").GetString().Should().Be(SystemRoles.SalesRep);
    }

    [Fact]
    public async Task Switch_EndsPreviousSession_SoItsRefreshTokenStopsWorking()
    {
        var token = TestContext.Current.CancellationToken;
        var home = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var other = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await MembershipTestData.AddMembershipAsync(_factory.Services, other.OrganizationId, home.UserId, SystemRoleSeed.SalesRepRoleId, _factory.Clock, token);

        var signedIn = await IdentityApi.SignInAsync(_factory, home.Email, token);
        using var client = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, signedIn.AccessToken);
        (await SwitchAsync(client, other.OrganizationId, signedIn.RefreshToken, token)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var anonymous = AuthTestClient.CreateClient(_factory, CultureCodes.English);
        (await AuthTestClient.RefreshAsync(anonymous, signedIn.RefreshToken, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Switch_WithRefreshTokenOfAnotherUser_IsRejected()
    {
        var token = TestContext.Current.CancellationToken;
        var home = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var other = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await MembershipTestData.AddMembershipAsync(_factory.Services, other.OrganizationId, home.UserId, SystemRoleSeed.SalesRepRoleId, _factory.Clock, token);

        var homeSession = await IdentityApi.SignInAsync(_factory, home.Email, token);
        var otherUserSession = await IdentityApi.SignInAsync(_factory, other.Email, token);
        using var client = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, homeSession.AccessToken);

        var response = await SwitchAsync(client, other.OrganizationId, otherUserSession.RefreshToken, token);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var anonymous = AuthTestClient.CreateClient(_factory, CultureCodes.English);
        (await AuthTestClient.RefreshAsync(anonymous, homeSession.RefreshToken, token)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Switch_ToOrganizationWithoutActiveMembership_IsNotFound()
    {
        var token = TestContext.Current.CancellationToken;
        var home = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var stranger = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);

        var signedIn = await IdentityApi.SignInAsync(_factory, home.Email, token);
        using var client = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, signedIn.AccessToken);
        var response = await SwitchAsync(client, stranger.OrganizationId, signedIn.RefreshToken, token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await AuthTestClient.ReadJsonAsync(response, token)).GetProperty("code").GetString().Should().Be(IdentityErrorCodes.OrganizationNotFound);
    }

    private static Task<HttpResponseMessage> SwitchAsync(HttpClient client, Guid organizationId, string refreshToken, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(IdentityApi.Route(IdentityRoutes.OrganizationSwitch, organizationId), new { refreshToken }, cancellationToken);

    [Fact]
    public async Task TwoOrganizations_KeepTheirDataIsolated()
    {
        var token = TestContext.Current.CancellationToken;
        var first = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var second = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var firstAdmin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, first.AccessToken);
        using var secondAdmin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, second.AccessToken);

        var created = await firstAdmin.PostAsJsonAsync(IdentityRoutes.Teams, new { name = "Only in first" }, token);
        created.EnsureSuccessStatusCode();

        var secondTeams = await AuthTestClient.ReadJsonAsync(await secondAdmin.GetAsync($"{IdentityRoutes.Teams}?limit=200", token), token);
        secondTeams.GetProperty("items").EnumerateArray().Should().BeEmpty();

        var firstTeams = await AuthTestClient.ReadJsonAsync(await firstAdmin.GetAsync($"{IdentityRoutes.Teams}?limit=200", token), token);
        firstTeams.GetProperty("items").EnumerateArray().Select(team => team.GetProperty("name").GetString())
            .Should().Equal("Only in first");
    }
}
