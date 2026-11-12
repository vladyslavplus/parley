using System.Net;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
using Parley.Api.Controllers.Telegram;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Notifications.Telegram.Support;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Telegram.Webhook;
using Xunit;

namespace Parley.IntegrationTests.Notifications.Telegram;

/// <summary>
/// The bot end to end through the webhook: the secret check, the link and the unlink, a replayed update that does nothing, refused
/// codes, the priority answer under the membership, a deactivated member who loses the link, and an app with no token that still runs.
/// </summary>
public sealed class TelegramBotIntegrationTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const long OwnerChat = 500;

    private const long SecondChat = 501;

    private const string StatusEnabledProperty = "enabled";

    private const string StatusModeProperty = "mode";

    private const string StatusBotUsernameProperty = "botUsername";

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = TelegramHost.Create(postgres, mailpit);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Webhook_RefusesAMissingOrWrongSecret_AndAnswersTheRightOne()
    {
        var token = TestContext.Current.CancellationToken;

        (await TelegramHost.PostUpdateAsync(_factory, 1, OwnerChat, "/help", secret: null, cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TelegramHost.PostUpdateAsync(_factory, 2, OwnerChat, "/help", secret: "not-the-secret", cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        TelegramHost.Bot(_factory).Sent.Should().BeEmpty("a refused update never reaches the handler");

        (await TelegramHost.PostUpdateAsync(_factory, 3, OwnerChat, "/help", cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        TelegramHost.LastText(_factory).Should().Contain("Commands:");
    }

    [Fact]
    public async Task Webhook_RefusesAnOversizedBodyWithAWrongSecret_BeforeTheBodyIsRead()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, TelegramRoutes.Webhook)
        {
            // Well above the body limit: the secret is checked first, so the body is never read or parsed.
            Content = new ByteArrayContent(new byte[TelegramLimits.WebhookMaximumBodyBytes * 2]),
        };
        request.Headers.Add(TelegramWebhookSecret.HeaderName, "not-the-secret");

        using var response = await client.SendAsync(request, token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        TelegramHost.Bot(_factory).Sent.Should().BeEmpty("a refused request never reaches the handler");
    }

    [Fact]
    public async Task LinkThenUnlink_TurnsTheChatOnAndOff_AndTheStatusSaysSo()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var member = CrmApi.Client(_factory, owner.AccessToken);

        using var status = await member.GetAsync(TelegramRoutes.Status, token);
        var statusBody = await AuthTestClient.ReadJsonAsync(status, token);
        statusBody.GetProperty(StatusEnabledProperty).GetBoolean().Should().BeTrue();
        statusBody.GetProperty(StatusModeProperty).GetString().Should().Be("webhook");
        statusBody.GetProperty(StatusBotUsernameProperty).GetString().Should().Be(TelegramHost.BotUsername);

        await TelegramHost.LinkChatAsync(_factory, member, updateId: 1, OwnerChat, token);
        (await TelegramHost.IsLinkedAsync(member, token)).Should().BeTrue();

        (await TelegramHost.PostUpdateAsync(_factory, 2, OwnerChat, "/unlink", cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        TelegramHost.LastText(_factory).Should().Contain("The link is ended");
        (await TelegramHost.IsLinkedAsync(member, token)).Should().BeFalse();
    }

    [Fact]
    public async Task ReplayedUpdate_HasNoEffect()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var member = CrmApi.Client(_factory, owner.AccessToken);

        await TelegramHost.LinkChatAsync(_factory, member, updateId: 1, OwnerChat, token);
        (await TelegramHost.PostUpdateAsync(_factory, 2, OwnerChat, "/unlink", cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await TelegramHost.IsLinkedAsync(member, token)).Should().BeFalse();

        await TelegramHost.LinkChatAsync(_factory, member, updateId: 3, OwnerChat, token);
        var sentBeforeReplay = TelegramHost.Bot(_factory).Sent.Count;

        (await TelegramHost.PostUpdateAsync(_factory, 2, OwnerChat, "/unlink", cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK, "a replayed update is acknowledged");

        (await TelegramHost.IsLinkedAsync(member, token)).Should().BeTrue("the replayed /unlink must not end the link again");
        TelegramHost.Bot(_factory).Sent.Count.Should().Be(sentBeforeReplay, "a replayed update sends no reply");
    }

    [Fact]
    public async Task UsedAndExpiredCodes_AreRefused()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var member = CrmApi.Client(_factory, owner.AccessToken);

        var usedCode = await TelegramHost.CreateLinkCodeAsync(member, token);
        (await TelegramHost.PostUpdateAsync(_factory, 1, OwnerChat, "/start " + usedCode, cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        TelegramHost.LastText(_factory).Should().Contain("This chat is linked");

        (await TelegramHost.PostUpdateAsync(_factory, 2, SecondChat, "/start " + usedCode, cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        TelegramHost.LastText(_factory).Should().Contain("invalid", "a used code is refused");
        (await TelegramHost.CountActiveLinksAsync(_factory, SecondChat, token)).Should().Be(0);

        (await TelegramHost.PostUpdateAsync(_factory, 3, OwnerChat, "/unlink", cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var expiredCode = await TelegramHost.CreateLinkCodeAsync(member, token);
        await TelegramHost.ExpireCodesAsync(_factory, owner.UserId, token);

        (await TelegramHost.PostUpdateAsync(_factory, 4, SecondChat, "/start " + expiredCode, cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        TelegramHost.LastText(_factory).Should().Contain("invalid", "an expired code is refused");
        (await TelegramHost.CountActiveLinksAsync(_factory, SecondChat, token)).Should().Be(0);
    }

    [Fact]
    public async Task Today_AnswersWithThePriorityList_ForAnActiveMember()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var member = CrmApi.Client(_factory, owner.AccessToken);
        await TelegramHost.LinkChatAsync(_factory, member, updateId: 1, OwnerChat, token);

        TelegramHost.Priority(_factory).Entries.Add(TestEntries.Action(owner.UserId, "action.call"));
        TelegramHost.Priority(_factory).Entries.Add(TestEntries.Action(null, "action.nurture"));

        (await TelegramHost.PostUpdateAsync(_factory, 2, OwnerChat, "/today", cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var reply = TelegramHost.LastText(_factory);
        reply.Should().Contain("1. Call the contact.");
        reply.Should().Contain("2. Nurture the contact.");
        reply.Should().Contain("https://app.parley.test/en/insights", "the answer links to the insights page, not to a contact");
    }

    [Fact]
    public async Task Today_GivesNothing_AndEndsTheLink_WhenTheMembershipWasDeactivated()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        await TelegramHost.LinkChatAsync(_factory, repClient, updateId: 1, SecondChat, token);
        TelegramHost.Priority(_factory).Entries.Add(TestEntries.Action(rep.UserId, "action.call"));

        var membershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, rep.UserId, token);
        (await admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, membershipId), null, token))
            .IsSuccessStatusCode.Should().BeTrue();

        (await TelegramHost.PostUpdateAsync(_factory, 2, SecondChat, "/today", cancellationToken: token))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        TelegramHost.LastText(_factory).Should().Contain("membership in this organization has ended");
        TelegramHost.LastText(_factory).Should().NotContain("Call the contact.", "a deactivated member gets no list");
        (await TelegramHost.CountActiveLinksAsync(_factory, SecondChat, token)).Should().Be(0, "the link is treated as inactive");
    }

    [Fact]
    public async Task WithoutAToken_TheAppRuns_AndTheChannelIsOff()
    {
        var token = TestContext.Current.CancellationToken;
        using var factory = TelegramHost.CreateWithoutToken(postgres, mailpit);
        var owner = await IdentitySetup.CreateOwnerAsync(factory, mailpit, token);
        using var member = CrmApi.Client(factory, owner.AccessToken);

        using var status = await member.GetAsync(TelegramRoutes.Status, token);
        status.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await AuthTestClient.ReadJsonAsync(status, token);
        body.GetProperty(StatusEnabledProperty).GetBoolean().Should().BeFalse();
        body.GetProperty(StatusBotUsernameProperty).ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);

        using var link = await member.PostAsync(TelegramRoutes.LinkCode, null, token);
        link.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CrmApi.ErrorCodeAsync(link, token)).Should().Be(TelegramErrorCodes.NotConfigured);
    }
}
