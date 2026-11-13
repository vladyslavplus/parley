using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Parley.Api.Controllers.Identity;
using Parley.Api.Controllers.Notifications;
using Parley.Crm.Persistence;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Notifications.Telegram.Support;
using Parley.IntegrationTests.Outbox.Support;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Options;
using Parley.Notifications.Telegram.Delivery;
using Xunit;

namespace Parley.IntegrationTests.Notifications.Telegram;

/// <summary>
/// The Telegram delivery of an assigned task: nothing is sent while the switch is off, a message is sent once to a linked chat even
/// when the event is replayed, a blocked chat loses its links, and a rate limit schedules the retry instead of sending now.
/// </summary>
public sealed class TelegramDeliveryIntegrationTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string VersionProperty = "version";

    private const string TaskTitle = "Call back";

    private const long RepChat = 1100;

    private const string StatusPending = TelegramDeliveryStatuses.Pending;

    private const string StatusBlocked = TelegramDeliveryStatuses.Blocked;

    private const string StatusFailed = TelegramDeliveryStatuses.Failed;

    /// <summary>A one-letter culture name no platform accepts; the column is short, so the value stays within it.</summary>
    private const string UnknownCulture = "x";

    private const string CorruptLinkLocaleSql =
        "UPDATE notifications.telegram_links SET locale = @locale WHERE user_id = @user AND active";

    private const string MakeDeliveryDueSql =
        "UPDATE notifications.telegram_deliveries SET claimed_until = NULL, next_attempt_at = '2000-01-01T00:00:00Z' WHERE id = @id";

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
    public async Task WhileTheSwitchIsOff_NothingIsSent_AndNoDeliveryIsStaged()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        await TelegramHost.LinkChatAsync(_factory, repClient, updateId: 1, RepChat, token);

        await AssignTaskAsync(owner, rep.UserId, token);
        await Sweep(token);

        (await TelegramHost.ReadDeliveriesAsync(_factory, token)).Should().BeEmpty("the telegram switch is off by default");
        TelegramHost.Bot(_factory).Sent.Should().ContainSingle("only the link reply was sent");
    }

    [Fact]
    public async Task WithTheSwitchOn_TheMessageIsSentOnce_AndAReplayedEventAddsNothing()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        await TelegramHost.LinkChatAsync(_factory, repClient, updateId: 1, RepChat, token);
        await EnableTaskAssignedOnTelegramAsync(repClient, token);

        await AssignTaskAsync(owner, rep.UserId, token);
        await Sweep(token);

        var message = TelegramHost.Bot(_factory).Sent
            .Should().Contain(sent => sent.ChatId == RepChat && sent.Text.StartsWith("A task was assigned", StringComparison.Ordinal))
            .Which;
        message.Text.Should().Contain("A task was assigned to you.");
        message.Text.Should().Contain("https://app.parley.test/en/app/tasks", "the link opens the task list of the web app");
        var sentBefore = TelegramHost.Bot(_factory).Sent.Count;

        await TelegramHost.ReplayTaskAssignedAsync(postgres, token);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);
        await Sweep(token);

        TelegramHost.Bot(_factory).Sent.Count.Should().Be(sentBefore, "the replayed event must not send a second message");
        (await TelegramHost.ReadDeliveriesAsync(_factory, token)).Should().ContainSingle()
            .Which.Status.Should().Be(TelegramDeliveryStatuses.Sent);
    }

    [Fact]
    public async Task ABlockedChat_LosesItsLink_AndItsDeliveryIsMarkedBlocked()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        await TelegramHost.LinkChatAsync(_factory, repClient, updateId: 1, RepChat, token);
        await EnableTaskAssignedOnTelegramAsync(repClient, token);
        TelegramHost.Bot(_factory).FailNextSend(RepChat, statusCode: 403);

        await AssignTaskAsync(owner, rep.UserId, token);
        await Sweep(token);

        (await TelegramHost.ReadDeliveriesAsync(_factory, token)).Should().ContainSingle()
            .Which.Status.Should().Be(StatusBlocked);
        (await TelegramHost.CountActiveLinksAsync(_factory, RepChat, token)).Should().Be(0);
    }

    [Fact]
    public async Task ADeactivatedMember_GetsNoDelivery_AndTheirLinkEnds()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        await TelegramHost.LinkChatAsync(_factory, repClient, updateId: 1, RepChat, token);
        await EnableTaskAssignedOnTelegramAsync(repClient, token);
        await AssignTaskAsync(owner, rep.UserId, token);

        var membershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, rep.UserId, token);
        (await admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, membershipId), null, token))
            .IsSuccessStatusCode.Should().BeTrue();
        var sentBefore = TelegramHost.Bot(_factory).Sent.Count;

        await OutboxDelivery.DeliverDueAsync<IdentityDbContext>(_factory.Services, token);
        await Sweep(token);

        TelegramHost.Bot(_factory).Sent.Count.Should().Be(sentBefore, "a deactivated member is sent nothing");
        (await TelegramHost.ReadDeliveriesAsync(_factory, token)).Should().ContainSingle()
            .Which.Status.Should().Be(TelegramDeliveryStatuses.Cancelled);
        (await TelegramHost.CountActiveLinksAsync(_factory, RepChat, token)).Should().Be(0, "the link ends with the membership");
    }

    [Fact]
    public async Task ATelegramRateLimit_SchedulesTheRetry_InsteadOfSendingNow()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        await TelegramHost.LinkChatAsync(_factory, repClient, updateId: 1, RepChat, token);
        await EnableTaskAssignedOnTelegramAsync(repClient, token);
        TelegramHost.Bot(_factory).FailNextSend(RepChat, statusCode: 429, retryAfter: TimeSpan.FromMinutes(5));

        await AssignTaskAsync(owner, rep.UserId, token);
        var claimed = await Sweep(token);

        claimed.Should().Be(1);
        var delivery = (await TelegramHost.ReadDeliveriesAsync(_factory, token)).Should().ContainSingle().Subject;
        delivery.Status.Should().Be(StatusPending);
        delivery.Attempts.Should().Be(1);
        delivery.ErrorCode.Should().Be(TelegramErrorCodes.DeliveryRateLimited);
        delivery.NextAttemptAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(4), "Telegram's retry time is honored");
        (await Sweep(token)).Should().Be(0, "a delivery scheduled for later is not due yet");
    }

    [Fact]
    public async Task ACrashingDelivery_CountsEveryAttempt_AndFailsAfterTheLimit()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        await TelegramHost.LinkChatAsync(_factory, repClient, updateId: 1, RepChat, token);
        await EnableTaskAssignedOnTelegramAsync(repClient, token);
        await AssignTaskAsync(owner, rep.UserId, token);

        // The message text cannot be built (the link culture is unreadable), so every attempt crashes after it was counted
        // and before it was sent.
        var delivery = (await TelegramHost.ReadDeliveriesAsync(_factory, token)).Should().ContainSingle().Subject;
        await ExecuteAsync(CorruptLinkLocaleSql, ("locale", UnknownCulture), ("user", rep.UserId), token);
        var maxAttempts = _factory.Services.GetRequiredService<IOptions<TelegramOptions>>().Value.DeliveryMaxAttempts;
        var sentBefore = TelegramHost.Bot(_factory).Sent.Count;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await ExecuteAsync(MakeDeliveryDueSql, ("id", delivery.Id), token);
            await Sweep(token);
            (await TelegramHost.ReadDeliveriesAsync(_factory, token)).Single(row => row.Id == delivery.Id)
                .Attempts.Should().Be(attempt, "the attempt is counted before the send, so a crash still uses it up");
        }

        // The next claim finds no attempt left and fails the row instead of retrying for ever.
        await ExecuteAsync(MakeDeliveryDueSql, ("id", delivery.Id), token);
        await Sweep(token);

        var failed = (await TelegramHost.ReadDeliveriesAsync(_factory, token)).Single(row => row.Id == delivery.Id);
        failed.Status.Should().Be(StatusFailed);
        failed.Attempts.Should().Be(maxAttempts);
        failed.ErrorCode.Should().Be(TelegramErrorCodes.DeliveryUnavailable);
        TelegramHost.Bot(_factory).Sent.Count.Should().Be(sentBefore, "nothing was ever sent");
    }

    private async Task<int> Sweep(CancellationToken cancellationToken) =>
        await _factory.Services.GetRequiredService<TelegramDeliverySweep>().RunOnceAsync(cancellationToken);

    private async Task ExecuteAsync(string sql, (string Name, object Value) first, CancellationToken cancellationToken) =>
        await ExecuteAsync(sql, first, second: null, cancellationToken: cancellationToken);

    private async Task ExecuteAsync(
        string sql,
        (string Name, object Value) first,
        (string Name, object Value)? second,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(first.Name, first.Value);
        if (second is { } other)
        {
            command.Parameters.AddWithValue(other.Name, other.Value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task AssignTaskAsync(OrganizationOwner owner, Guid assigneeUserId, CancellationToken cancellationToken)
    {
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        await TaskApi.CreateAsync(admin, TaskTitle, null, assigneeUserId, null, cancellationToken);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, cancellationToken);
    }

    private static async Task EnableTaskAssignedOnTelegramAsync(HttpClient member, CancellationToken cancellationToken)
    {
        var read = await member.GetAsync(NotificationsRoutes.Preferences, cancellationToken);
        var version = (await AuthTestClient.ReadJsonAsync(read, cancellationToken)).GetProperty(VersionProperty).GetInt64();

        var change = new
        {
            version,
            items = new[] { new { type = NotificationTypes.TaskAssigned, channel = NotificationChannels.Telegram, enabled = true } },
        };
        var response = await member.PutAsJsonAsync(NotificationsRoutes.Preferences, change, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
