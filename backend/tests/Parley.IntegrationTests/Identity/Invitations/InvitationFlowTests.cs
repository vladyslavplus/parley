using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
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

namespace Parley.IntegrationTests.Identity.Invitations;

/// <summary>
/// The invitation lifecycle through the API and the mailbox: the email arrives in the
/// requested language, the link accepts once, expired and revoked links fail, and the
/// token is stored only as a hash.
/// </summary>
public sealed class InvitationFlowTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string UkInvitationSubject = "Запрошення до Parley";

    private const string EnInvitationSubject = "You are invited to Parley";

    private const string InvitationLinkPattern = @"token=(?<token>\S+)";

    private static readonly TimeSpan PastLifetime = TimeSpan.FromDays(8);

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
    public async Task Invitation_EmailInRequestedLanguage_AcceptsOnce_AndStoresOnlyTheHash()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.Ukrainian, owner.AccessToken);
        var invitee = AuthTestClient.UniqueEmail();

        var invited = await admin.PostAsJsonAsync(IdentityRoutes.Invitations, new { email = invitee, roleCode = SystemRoles.SalesRep }, token);
        invited.EnsureSuccessStatusCode();

        var message = await WaitForEmailAsync(mailpit, invitee, token, UkInvitationSubject);
        var invitationToken = ReadInvitationToken(message.Text);

        var storedHashes = await MembershipTestData.ReadInvitationHashesAsync(_factory.Services, owner.OrganizationId, token);
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(invitationToken));
        storedHashes.Should().Contain(hash => hash.SequenceEqual(expectedHash));
        storedHashes.Should().NotContain(hash => hash.SequenceEqual(Encoding.UTF8.GetBytes(invitationToken)));

        using var anonymous = CreateClient(_factory, CultureCodes.Ukrainian);
        var accepted = await anonymous.PostAsJsonAsync(
            IdentityRoutes.InvitationAccept,
            new { token = invitationToken, password = Password },
            token);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);

        var replayed = await anonymous.PostAsJsonAsync(
            IdentityRoutes.InvitationAccept,
            new { token = invitationToken, password = Password },
            token);
        replayed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(replayed, token)).GetProperty("code").GetString().Should().Be(IdentityErrorCodes.InvitationTokenInvalid);

        var inviteeTokens = await IdentityApi.SignInAsync(_factory, invitee, token);
        IdentityApi.ReadOrganizationClaim(inviteeTokens.AccessToken).Should().Be(owner.OrganizationId.ToString("D"));
    }

    [Fact]
    public async Task Invitation_AfterItsLifetime_CannotBeAccepted()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);
        var invitee = AuthTestClient.UniqueEmail();
        (await admin.PostAsJsonAsync(IdentityRoutes.Invitations, new { email = invitee, roleCode = SystemRoles.Viewer }, token)).EnsureSuccessStatusCode();
        var invitationToken = ReadInvitationToken((await WaitForEmailAsync(mailpit, invitee, token, EnInvitationSubject)).Text);

        _factory.Clock.Advance(PastLifetime);

        using var anonymous = CreateClient(_factory, CultureCodes.English);
        var response = await anonymous.PostAsJsonAsync(IdentityRoutes.InvitationAccept, new { token = invitationToken, password = Password }, token);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ReadJsonAsync(response, token)).GetProperty("code").GetString().Should().Be(IdentityErrorCodes.InvitationTokenInvalid);
    }

    [Fact]
    public async Task Invitation_AfterRevocation_CannotBeAccepted()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);
        var invitee = AuthTestClient.UniqueEmail();
        var invited = await admin.PostAsJsonAsync(IdentityRoutes.Invitations, new { email = invitee, roleCode = SystemRoles.Marketer }, token);
        invited.EnsureSuccessStatusCode();
        var invitationId = (await ReadJsonAsync(invited, token)).GetProperty("id").GetGuid();
        var invitationToken = ReadInvitationToken((await WaitForEmailAsync(mailpit, invitee, token, EnInvitationSubject)).Text);

        var revoked = await admin.PostAsync(IdentityApi.Route(IdentityRoutes.InvitationRevoke, invitationId), null, token);
        revoked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var anonymous = CreateClient(_factory, CultureCodes.English);
        var response = await anonymous.PostAsJsonAsync(IdentityRoutes.InvitationAccept, new { token = invitationToken, password = Password }, token);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invitation_InvitedAgainWhilePending_RefreshesTheSameInvitation()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);
        var invitee = AuthTestClient.UniqueEmail();

        var first = await admin.PostAsJsonAsync(IdentityRoutes.Invitations, new { email = invitee, roleCode = SystemRoles.Viewer }, token);
        first.EnsureSuccessStatusCode();
        var firstId = (await ReadJsonAsync(first, token)).GetProperty("id").GetGuid();

        var second = await admin.PostAsJsonAsync(IdentityRoutes.Invitations, new { email = invitee, roleCode = SystemRoles.SalesRep }, token);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await ReadJsonAsync(second, token);
        refreshed.GetProperty("id").GetGuid().Should().Be(firstId);
        refreshed.GetProperty("roleCode").GetString().Should().Be(SystemRoles.SalesRep);

        var pendingRows = await MembershipTestData.ReadInvitationHashesAsync(_factory.Services, owner.OrganizationId, token);
        pendingRows.Should().HaveCount(1);
    }

    [Fact]
    public async Task Invitation_ForUnconfirmedRegistration_AcceptedWithTheInvitedPassword()
    {
        const string squatterPassword = "Squatter-password-1";
        const string ownerChosenPassword = "Owner-chosen-2";
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);
        var invitee = AuthTestClient.UniqueEmail();

        // Someone registers the address first and never confirms it.
        using var squatter = CreateClient(_factory, CultureCodes.English);
        (await RegisterAsync(squatter, invitee, squatterPassword, token)).EnsureSuccessStatusCode();

        (await admin.PostAsJsonAsync(IdentityRoutes.Invitations, new { email = invitee, roleCode = SystemRoles.SalesRep }, token)).EnsureSuccessStatusCode();
        var invitationToken = ReadInvitationToken((await WaitForEmailAsync(mailpit, invitee, token, EnInvitationSubject)).Text);

        using var anonymous = CreateClient(_factory, CultureCodes.English);
        var accepted = await anonymous.PostAsJsonAsync(
            IdentityRoutes.InvitationAccept,
            new { token = invitationToken, password = ownerChosenPassword },
            token);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);

        (await SignInAsync(anonymous, invitee, squatterPassword, token)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await SignInAsync(anonymous, invitee, ownerChosenPassword, token)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string ReadInvitationToken(string mailText)
    {
        var match = Regex.Match(mailText, InvitationLinkPattern, RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            throw new InvalidOperationException("The invitation email carries no link.");
        }

        return Uri.UnescapeDataString(match.Groups["token"].Value);
    }
}
