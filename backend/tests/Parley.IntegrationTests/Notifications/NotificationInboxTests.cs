using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Api.Controllers.Identity;
using Parley.Api.Controllers.Notifications;
using Parley.Campaigns.Contracts.Events;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Persistence;
using Parley.Crm.Tasks;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Events;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Outbox.Consumers;
using Parley.SharedKernel.Outbox.Messages;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Notifications;

/// <summary>
/// In-app notifications end to end: an event becomes one row for its addressee, a replayed event adds nothing, a disabled
/// type adds nothing, only the addressee reads their notifications, and a stale preference write is refused.
/// </summary>
public sealed class NotificationInboxTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ItemsProperty = "items";

    private const string IdProperty = "id";

    private const string TypeProperty = "type";

    private const string LinkRouteProperty = "linkRoute";

    private const string IsReadProperty = "isRead";

    private const string CountProperty = "count";

    private const string MarkedProperty = "marked";

    private const string VersionProperty = "version";

    private const string EnabledProperty = "enabled";

    private const string IdPlaceholder = "{id:guid}";

    /// <summary>Copies the task-assigned outbox rows as new unprocessed rows: a replay of the same events.</summary>
    private const string ReplayAssignmentsSql = """
        INSERT INTO crm.outbox_messages (id, organization_id, type, payload, occurred_at, processed_at, attempts, next_attempt_at)
        SELECT gen_random_uuid(), organization_id, type, payload, occurred_at, NULL, 0, now()
        FROM crm.outbox_messages
        WHERE type = @type
        """;

    private const string ParameterType = "type";

    private IdentityApiFactory _factory = null!;

    private string _connectionString = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _connectionString = postgres.ConnectionString;
        _factory = new IdentityApiFactory(postgres, mailpit);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AssignedTask_NotifiesTheAssignee_AndAReplayedEventAddsNothing()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        await TaskApi.CreateAsync(admin, "Call back", null, rep.UserId, null, token);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);

        var items = await ReadItemsAsync(repClient, token);
        items.Should().ContainSingle();
        items[0].GetProperty(TypeProperty).GetString().Should().Be(NotificationTypes.TaskAssigned);
        items[0].GetProperty(LinkRouteProperty).GetString().Should().Be(NotificationLinkRoutes.Tasks);

        await ReplayAsync(typeof(TaskAssigned).FullName!, token);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);

        (await ReadItemsAsync(repClient, token)).Should().ContainSingle("the replayed event must not create a second row");
    }

    [Fact]
    public async Task SelfAssignedTask_NotifiesNobody()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        await TaskApi.CreateAsync(admin, "Mine", null, owner.UserId, null, token);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);

        (await ReadItemsAsync(admin, token)).Should().BeEmpty("the actor is never told about their own change");
    }

    [Fact]
    public async Task DisabledType_CreatesNoRow_AndTheVersionMovesWithTheSwitch()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);

        var before = await ReadPreferencesAsync(repClient, token);
        var switched = await repClient.PutAsJsonAsync(
            NotificationsRoutes.Preferences,
            new { version = before.GetProperty(VersionProperty).GetInt64(), items = new[] { new { type = NotificationTypes.TaskAssigned, channel = NotificationChannels.InApp, enabled = false } } },
            token);
        switched.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await AuthTestClient.ReadJsonAsync(switched, token);
        after.GetProperty(VersionProperty).GetInt64().Should().NotBe(before.GetProperty(VersionProperty).GetInt64());

        await TaskApi.CreateAsync(admin, "Muted", null, rep.UserId, null, token);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);

        (await ReadItemsAsync(repClient, token)).Should().BeEmpty("a disabled type creates no in-app row");
    }

    [Fact]
    public async Task DueSoonReminder_ReachesTheAssigneeOnce_AcrossRepeatedSweeps()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        await TaskApi.CreateAsync(admin, "Soon", _factory.Clock.GetUtcNow().AddMinutes(30), rep.UserId, null, token);

        await RunSweepAsync(token);
        await RunJobAsync(owner.OrganizationId, token);
        await RunJobAsync(owner.OrganizationId, token);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);

        var reminders = (await ReadItemsAsync(repClient, token))
            .Where(item => item.GetProperty(TypeProperty).GetString() == NotificationTypes.TaskDueSoon)
            .ToList();
        reminders.Should().ContainSingle("the reminder is claimed once, so repeated sweeps add nothing");
    }

    [Fact]
    public async Task OnlyTheAddressee_CanReadOrMarkTheirNotifications_AndCountsFollow()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var colleague = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var otherOrganization = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        using var colleagueClient = CrmApi.Client(_factory, colleague.AccessToken);
        using var strangerClient = CrmApi.Client(_factory, otherOrganization.AccessToken);
        await TaskApi.CreateAsync(admin, "First", null, rep.UserId, null, token);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);
        var notificationId = (await ReadItemsAsync(repClient, token)).Single().GetProperty(IdProperty).GetGuid();

        var fromColleague = await colleagueClient.PostAsync(MarkReadRoute(notificationId), null, token);
        fromColleague.StatusCode.Should().Be(HttpStatusCode.NotFound, "another user of the organization gets not found, not forbidden");
        (await CrmApi.ErrorCodeAsync(fromColleague, token)).Should().Be(NotificationErrorCodes.NotificationNotFound);
        (await strangerClient.PostAsync(MarkReadRoute(notificationId), null, token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ReadItemsAsync(colleagueClient, token)).Should().BeEmpty();

        (await ReadUnreadCountAsync(repClient, token)).Should().Be(1);
        var read = await repClient.PostAsync(MarkReadRoute(notificationId), null, token);
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AuthTestClient.ReadJsonAsync(read, token)).GetProperty(IsReadProperty).GetBoolean().Should().BeTrue();
        (await ReadUnreadCountAsync(repClient, token)).Should().Be(0);

        await TaskApi.CreateAsync(admin, "Second", null, rep.UserId, null, token);
        await TaskApi.CreateAsync(admin, "Third", null, rep.UserId, null, token);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);
        (await ReadUnreadCountAsync(repClient, token)).Should().Be(2);

        var readAll = await repClient.PostAsync(NotificationsRoutes.MarkAllRead, null, token);
        readAll.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AuthTestClient.ReadJsonAsync(readAll, token)).GetProperty(MarkedProperty).GetInt32().Should().Be(2);
        (await ReadUnreadCountAsync(repClient, token)).Should().Be(0);
    }

    [Fact]
    public async Task StalePreferenceWrite_IsRefused_WithAConflict()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        var version = (await ReadPreferencesAsync(repClient, token)).GetProperty(VersionProperty).GetInt64();
        var change = new[] { new { type = NotificationTypes.DealAssigned, channel = NotificationChannels.InApp, enabled = false } };

        (await repClient.PutAsJsonAsync(NotificationsRoutes.Preferences, new { version, items = change }, token))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var stale = await repClient.PutAsJsonAsync(NotificationsRoutes.Preferences, new { version, items = change }, token);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(stale, token)).Should().Be(NotificationErrorCodes.PreferencesVersionConflict);
    }

    [Fact]
    public async Task DeactivatedMember_IsRefusedOnNotifications()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        (await repClient.GetAsync(NotificationsRoutes.List, token)).StatusCode.Should().Be(HttpStatusCode.OK);

        var membershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, rep.UserId, token);
        (await admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, membershipId), null, token))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await repClient.GetAsync(NotificationsRoutes.List, token)).StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "an inactive membership carries no permission, so the list is refused at once");
    }

    [Fact]
    public async Task CampaignCompletedConsumer_NotifiesTheStarter()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        await ConsumeAsync(
            owner.OrganizationId,
            new CampaignCompleted
            {
                CampaignId = Guid.NewGuid(),
                CompletedAt = _factory.Clock.GetUtcNow(),
                StartedByUserId = owner.UserId,
            },
            token);

        var items = await ReadItemsAsync(admin, token);
        items.Should().ContainSingle();
        items[0].GetProperty(TypeProperty).GetString().Should().Be(NotificationTypes.CampaignCompleted);
    }

    [Fact]
    public async Task MemberActivatedConsumer_NotifiesTheInviter_AndNeverTheNewMember()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);

        await ConsumeAsync(
            owner.OrganizationId,
            new MemberActivated { UserId = rep.UserId, InvitedByUserId = owner.UserId },
            token);

        var inviterItems = await ReadItemsAsync(admin, token);
        inviterItems.Should().ContainSingle();
        inviterItems[0].GetProperty(TypeProperty).GetString().Should().Be(NotificationTypes.MemberActivated);
        (await ReadItemsAsync(repClient, token)).Should().BeEmpty();
    }

    private async Task ConsumeAsync<TEvent>(Guid organizationId, TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        tenant.Set(organizationId);
        try
        {
            await scope.ServiceProvider.GetRequiredService<IIntegrationEventConsumer<TEvent>>()
                .ConsumeAsync(integrationEvent, cancellationToken);

            // The inbox dispatcher commits what a consumer stages; here the test commits it on the same context.
            await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().SaveChangesAsync(cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }

    private static string MarkReadRoute(Guid notificationId) =>
        NotificationsRoutes.MarkRead.Replace(IdPlaceholder, notificationId.ToString("D"));

    private static async Task<JsonElement> ReadPreferencesAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(NotificationsRoutes.Preferences, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    private static async Task<List<JsonElement>> ReadItemsAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(NotificationsRoutes.List, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty).EnumerateArray().ToList();
    }

    private static async Task<long> ReadUnreadCountAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(NotificationsRoutes.UnreadCount, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await AuthTestClient.ReadJsonAsync(response, cancellationToken)).GetProperty(CountProperty).GetInt64();
    }

    private async Task ReplayAsync(string eventType, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ReplayAssignmentsSql, connection);
        command.Parameters.AddWithValue(ParameterType, eventType);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TaskDueSoonScanJob>().ExecuteAsync(null, cancellationToken);
    }

    private async Task RunJobAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<TaskDueSoonJob>().ExecuteAsync(organizationId, cancellationToken);
    }
}
