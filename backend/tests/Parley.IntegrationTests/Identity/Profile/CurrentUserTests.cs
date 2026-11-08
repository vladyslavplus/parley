using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Domain.Users;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Identity.Profile;

/// <summary>
/// <c>GET /me</c> gives the interface its role, permissions and record scope. The data is
/// checked against the seeded matrix for every system role, and the profile update shows up in it.
/// </summary>
public sealed class CurrentUserTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
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

    [Theory]
    [InlineData(SystemRoles.Admin)]
    [InlineData(SystemRoles.SalesManager)]
    [InlineData(SystemRoles.SalesRep)]
    [InlineData(SystemRoles.Marketer)]
    [InlineData(SystemRoles.Viewer)]
    public async Task GetMe_ReturnsRoleAndPermissionsWithScope_OfTheSeededRole(string roleCode)
    {
        var token = TestContext.Current.CancellationToken;
        var roleId = SystemRoleSeed.Roles.Single(role => role.Code == roleCode).Id;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var (_, memberEmail) = await IdentitySetup.AddMemberAsync(_factory, owner.OrganizationId, roleId, token);
        var tokens = await IdentityApi.SignInAsync(_factory, memberEmail, token);

        using var client = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, tokens.AccessToken);
        var response = await client.GetAsync(IdentityRoutes.Me, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await AuthTestClient.ReadJsonAsync(response, token);

        me.GetProperty("roleCode").GetString().Should().Be(roleCode);
        var actual = me.GetProperty("permissions").EnumerateArray()
            .Select(grant => (Permission: grant.GetProperty("permission").GetString()!, Scope: grant.GetProperty("scope").GetString()))
            .ToArray();
        var expected = SystemRoleSeed.RolePermissions
            .Where(grant => grant.RoleId == roleId)
            .Select(grant => (Permission: grant.Permission, Scope: grant.Scope))
            .ToArray();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ProfileUpdate_IsReturnedByGetMe()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var client = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);

        var updated = await client.PutAsJsonAsync(
            IdentityRoutes.MeProfile,
            new { displayName = "Olena", locale = CultureCodes.Ukrainian, timeZone = "Europe/Kyiv", theme = UserThemes.Dark },
            token);
        updated.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var me = await AuthTestClient.ReadJsonAsync(await client.GetAsync(IdentityRoutes.Me, token), token);
        var profile = me.GetProperty("user");
        profile.GetProperty("displayName").GetString().Should().Be("Olena");
        profile.GetProperty("locale").GetString().Should().Be(CultureCodes.Ukrainian);
        profile.GetProperty("timeZone").GetString().Should().Be("Europe/Kyiv");
        profile.GetProperty("theme").GetString().Should().Be(UserThemes.Dark);
    }
}
