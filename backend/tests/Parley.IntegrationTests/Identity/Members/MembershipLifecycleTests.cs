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
using static Parley.IntegrationTests.Infrastructure.Auth.AuthTestClient;

namespace Parley.IntegrationTests.Identity.Members;

/// <summary>
/// Membership changes that must apply to the very next request: deactivation, role change,
/// the last-administrator guard, and the 404 answer for records of another organization.
/// </summary>
public sealed class MembershipLifecycleTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
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
    public async Task Deactivation_TakesEffectOnNextRequest_WithOldAccessToken()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var (memberId, memberEmail) = await IdentitySetup.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var memberTokens = await IdentityApi.SignInAsync(_factory, memberEmail, token);
        using var member = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, memberTokens.AccessToken);
        (await member.GetAsync(IdentityRoutes.Me, token)).StatusCode.Should().Be(HttpStatusCode.OK);

        var membershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, memberId, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);
        var deactivated = await admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, membershipId), null, token);
        deactivated.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The old access token is still signed and unexpired; only the membership changed.
        (await member.GetAsync(IdentityRoutes.Me, token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var anonymous = CreateClient(_factory, CultureCodes.English);
        (await RefreshAsync(anonymous, memberTokens.RefreshToken, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await SignInAsync(anonymous, memberEmail, Password, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RoleChange_TakesEffectOnNextRequest_WithOldAccessToken()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var (memberId, memberEmail) = await IdentitySetup.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var memberTokens = await IdentityApi.SignInAsync(_factory, memberEmail, token);
        using var member = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, memberTokens.AccessToken);

        var membershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, memberId, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);
        var changed = await admin.PutAsJsonAsync(
            IdentityApi.Route(IdentityRoutes.MemberRole, membershipId),
            new { roleCode = SystemRoles.Viewer },
            token);
        changed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await member.GetAsync(IdentityRoutes.Me, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await ReadJsonAsync(response, token);
        me.GetProperty("roleCode").GetString().Should().Be(SystemRoles.Viewer);
        var permissions = me.GetProperty("permissions").EnumerateArray()
            .Select(grant => grant.GetProperty("permission").GetString())
            .ToArray();
        permissions.Should().NotContain(Permissions.ContactsWrite);
    }

    [Fact]
    public async Task LastAdministrator_CannotBeDemotedOrDeactivated()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var ownerMembershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, owner.UserId, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);

        var demoted = await admin.PutAsJsonAsync(
            IdentityApi.Route(IdentityRoutes.MemberRole, ownerMembershipId),
            new { roleCode = SystemRoles.Viewer },
            token);
        demoted.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(demoted, token)).GetProperty("code").GetString().Should().Be(IdentityErrorCodes.MemberLastAdmin);

        var deactivated = await admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, ownerMembershipId), null, token);
        deactivated.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(deactivated, token)).GetProperty("code").GetString().Should().Be(IdentityErrorCodes.MemberLastAdmin);
    }

    [Fact]
    public async Task RecordsOfAnotherOrganization_AreNotFound()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);

        // A second organization with a team, a member and a pending invitation.
        var otherOwner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var otherAdmin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, otherOwner.AccessToken);
        var teamResponse = await otherAdmin.PostAsJsonAsync(IdentityRoutes.Teams, new { name = "Other team" }, token);
        teamResponse.EnsureSuccessStatusCode();
        var teamId = (await ReadJsonAsync(teamResponse, token)).GetProperty("id").GetGuid();
        var (memberUserId, _) = await IdentitySetup.AddMemberAsync(_factory, otherOwner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var memberId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, otherOwner.OrganizationId, memberUserId, token);
        var inviteResponse = await otherAdmin.PostAsJsonAsync(
            IdentityRoutes.Invitations,
            new { email = AuthTestClient.UniqueEmail(), roleCode = SystemRoles.SalesRep },
            token);
        inviteResponse.EnsureSuccessStatusCode();
        var invitationId = (await ReadJsonAsync(inviteResponse, token)).GetProperty("id").GetGuid();

        (await admin.GetAsync(IdentityApi.Route(IdentityRoutes.TeamById, teamId), token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PutAsJsonAsync(IdentityApi.Route(IdentityRoutes.TeamById, teamId), new { name = "Hijacked" }, token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.DeleteAsync(IdentityApi.Route(IdentityRoutes.TeamById, teamId), token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PutAsJsonAsync(IdentityApi.Route(IdentityRoutes.MemberRole, memberId), new { roleCode = SystemRoles.Admin }, token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, memberId), null, token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.PostAsync(IdentityApi.Route(IdentityRoutes.InvitationRevoke, invitationId), null, token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
