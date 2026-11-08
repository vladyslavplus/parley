using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Audit;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Audit.Shared;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;
using Parley.SharedKernel.Localization;
using Xunit;
using static Parley.IntegrationTests.Infrastructure.Auth.AuthTestClient;

namespace Parley.IntegrationTests.Identity.Audit;

/// <summary>
/// Every mutating member, team, invitation and organization action leaves exactly one
/// audit row once the outbox dispatcher has run, and no row carries personal data.
/// </summary>
public sealed class AuditTrailTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private static readonly IReadOnlyDictionary<string, int> ExpectedActions = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        [AuditActions.TeamCreated] = 1,
        [AuditActions.TeamRenamed] = 1,
        [AuditActions.TeamDeleted] = 1,
        [AuditActions.MemberTeamChanged] = 1,
        [AuditActions.MemberRoleChanged] = 1,
        [AuditActions.MemberDeactivated] = 1,
        [AuditActions.InvitationCreated] = 2,
        [AuditActions.InvitationRevoked] = 1,
        [AuditActions.InvitationAccepted] = 1,
        [AuditActions.OrganizationSettingsChanged] = 1,
    };

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
    public async Task MutatingActions_EachLeaveOneAuditRow_WithoutPersonalData()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var (memberUserId, _) = await IdentitySetup.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var memberId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, memberUserId, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);

        var teamId = (await ReadJsonAsync(await Send(admin.PostAsJsonAsync(IdentityRoutes.Teams, new { name = "Sales" }, token)), token))
            .GetProperty("id").GetGuid();
        await Send(admin.PutAsJsonAsync(IdentityApi.Route(IdentityRoutes.TeamById, teamId), new { name = "Sales EU" }, token));
        await Send(admin.PutAsJsonAsync(IdentityApi.Route(IdentityRoutes.MemberTeam, memberId), new { teamId }, token));
        await Send(admin.PutAsJsonAsync(IdentityApi.Route(IdentityRoutes.MemberRole, memberId), new { roleCode = SystemRoles.Viewer }, token));
        await Send(admin.PutAsJsonAsync(
            IdentityApi.Route(IdentityRoutes.OrganizationById, owner.OrganizationId),
            new { name = "Acme", timeZone = "Europe/Kyiv", defaultLocale = CultureCodes.Ukrainian, currency = "EUR" },
            token));

        var revokedInvite = await Send(admin.PostAsJsonAsync(IdentityRoutes.Invitations, new { email = AuthTestClient.UniqueEmail(), roleCode = SystemRoles.Viewer }, token));
        var revokedId = (await ReadJsonAsync(revokedInvite, token)).GetProperty("id").GetGuid();
        await Send(admin.PostAsync(IdentityApi.Route(IdentityRoutes.InvitationRevoke, revokedId), null, token));

        var inviteeEmail = AuthTestClient.UniqueEmail();
        await Send(admin.PostAsJsonAsync(IdentityRoutes.Invitations, new { email = inviteeEmail, roleCode = SystemRoles.SalesRep }, token));
        var inviteeToken = ReadTokenFromLink((await WaitForEmailAsync(mailpit, inviteeEmail, token)).Text);
        using var anonymous = CreateClient(_factory, CultureCodes.English);
        await Send(anonymous.PostAsJsonAsync(IdentityRoutes.InvitationAccept, new { token = inviteeToken, password = Password }, token));

        await Send(admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, memberId), null, token));
        await Send(admin.DeleteAsync(IdentityApi.Route(IdentityRoutes.TeamById, teamId), token));

        await DispatchOutboxAsync(token);

        var rows = await ReadAuditRowsAsync(owner.OrganizationId, token);
        rows.GroupBy(row => row.Action).ToDictionary(group => group.Key, group => group.Count())
            .Should().BeEquivalentTo(ExpectedActions);
        rows.Should().OnlyContain(row => row.Metadata == AuditMetadata.Empty);
        rows.Should().OnlyContain(row => row.ActorUserId != null);
        rows.Should().OnlyContain(row => !row.Action.Contains('@', StringComparison.Ordinal));
    }

    [Fact]
    public async Task SignInFailures_ForKnownAccount_AreAudited_AndLockoutIsAuditedOnce()
    {
        const int lockoutFailures = 5;
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var anonymous = CreateClient(_factory, CultureCodes.English);

        for (var attempt = 0; attempt < lockoutFailures; attempt++)
        {
            (await SignInAsync(anonymous, owner.Email, "Wrong-password-9", token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        await DispatchOutboxAsync(token);

        var rows = await ReadAuditRowsAsync(owner.OrganizationId, token);
        rows.Count(row => row.Action == AuditActions.SignInFailed).Should().Be(lockoutFailures);
        rows.Count(row => row.Action == AuditActions.AccountLocked).Should().Be(1);
        rows.Where(row => row.Action is AuditActions.SignInFailed or AuditActions.AccountLocked)
            .Should().OnlyContain(row => row.ActorUserId == owner.UserId && row.TargetType == AuditTargetTypes.User && row.Metadata == AuditMetadata.Empty);
    }

    private static async Task<HttpResponseMessage> Send(Task<HttpResponseMessage> request)
    {
        var response = await request;
        response.EnsureSuccessStatusCode();
        return response;
    }

    /// <summary>Runs the outbox dispatcher until no row is due, so audit consumers have written their rows.</summary>
    private Task DispatchOutboxAsync(CancellationToken cancellationToken) =>
        OutboxDelivery.DeliverDueAsync<IdentityDbContext>(_factory.Services, cancellationToken);

    private async Task<List<Parley.Identity.Domain.Audit.AuditEvent>> ReadAuditRowsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.AuditEvents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(row => row.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }

    private static string ReadTokenFromLink(string mailText)
    {
        var marker = "token=";
        var start = mailText.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = mailText.IndexOfAny([' ', '\r', '\n'], start);
        var value = end < 0 ? mailText[start..] : mailText[start..end];
        return Uri.UnescapeDataString(value);
    }
}
