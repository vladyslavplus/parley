using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Parley.Api.Controllers.Campaigns;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Features.Campaigns.Attribution;
using Parley.Campaigns.Features.Campaigns.Recipients;
using Parley.Campaigns.Sending.Mail;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Domain.Orders;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Errors;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Attribution;

/// <summary>
/// Attribution and the results read, end to end on the send pipeline with a recording mail sender. The tests cover the
/// open window, the in-window counting with the symmetric send time, the identical re-run, the closed window with no
/// difference, and the permission gates of the read and the refresh.
/// </summary>
public sealed class CampaignAttributionTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int HoldoutShareBps = 1000;

    private const int ContactCount = 8;

    /// <summary>Arm sizes pinned before the freeze: the lift is defined only when both arms are non-empty.</summary>
    private const int MinimumHoldout = 1;

    private const int MinimumTreatment = 1;

    private const string IdempotencyKey = "attribution-start";

    private const string ProblemCodeProperty = "code";

    private const string VerdictProperty = "verdict";

    private const string ItemsProperty = "items";

    private const string NextCursorProperty = "nextCursor";

    private const string RecipientIdProperty = "recipientId";

    private const string ContactIdProperty = "contactId";

    private const string ContactNameProperty = "contactName";

    private const string ArmProperty = "arm";

    private const string SendStatusProperty = "sendStatus";

    private const string ConversionProperty = "conversion";

    private const string EmailVisibleProperty = "emailVisible";

    private const string EmailProperty = "email";

    private const string HoldoutArm = HoldoutArms.Holdout;

    private const string WindowStateProperty = "windowState";

    private const string IsFinalProperty = "isFinal";

    private const string SimulatedProperty = "simulated";

    private const string LiftProperty = "lift";

    private const string TreatmentProperty = "treatment";

    private const string HoldoutProperty = "holdout";

    private const string ConvertedProperty = "converted";

    private const string WindowOpen = "open";

    private const string WindowClosed = "closed";

    private const string WindowAwaitingFinal = "awaiting_final";

    private const string VerdictMeasurableLift = "measurable_lift";

    private static readonly TimeSpan DaySixOfFourteen = TimeSpan.FromDays(5);

    private const string VerdictTooEarly = "too_early";

    private const string VerdictNoMeasurableEffect = "no_measurable_effect";

    private const string ParameterCampaign = "campaign";

    private const string ParameterOrganization = "org";

    private const string ParameterContact = "contact";

    private const string ParameterReference = "reference";

    private const string ParameterOrderedAt = "ordered";

    private const string ParameterAmount = "amount";

    private const string ParameterCurrency = "currency";

    private const string ParameterSource = "source";

    private const string CampaignStartedAtSql = "SELECT started_at FROM campaigns.campaigns WHERE id = @campaign";

    private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    private static readonly TimeSpan FifteenDays = TimeSpan.FromDays(15);

    private static readonly TimeSpan ClosedWindowAdvance = TimeSpan.FromDays(30);

    private static readonly decimal OrderAmount = 10m;

    private RecordingCampaignMailSender _sender = null!;

    private IdentityApiFactory _factory = null!;

    private string _connectionString = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _connectionString = postgres.ConnectionString;
        _sender = new RecordingCampaignMailSender();
        var sender = _sender;
        _factory = new IdentityApiFactory(
            postgres,
            mailpit,
            extraServices: services =>
            {
                services.RemoveAll<ICampaignMailSender>();
                services.AddSingleton<ICampaignMailSender>(sender);
            });
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Results_AreTooEarly_WhileTheWindowIsOpen()
    {
        var token = TestContext.Current.CancellationToken;
        var (organizationId, admin, campaignId) = await SendCampaignAsync(token);

        var before = await GetResultsAsync(admin, campaignId, token);
        before.GetProperty(VerdictProperty).GetString().Should().Be(VerdictTooEarly);
        before.GetProperty(WindowStateProperty).GetString().Should().Be(WindowOpen, "the window follows the completed campaign, not the attribution run");

        await RunAttributionAsync(organizationId, campaignId, token);

        var after = await GetResultsAsync(admin, campaignId, token);
        after.GetProperty(VerdictProperty).GetString().Should().Be(VerdictTooEarly);
        after.GetProperty(WindowStateProperty).GetString().Should().Be(WindowOpen);
        after.GetProperty(IsFinalProperty).GetBoolean().Should().BeFalse();
        after.GetProperty(SimulatedProperty).GetBoolean().Should().BeFalse("no simulated order exists");
    }

    [Fact]
    public async Task Attribution_CountsOnlyOrdersInTheWindow_AndRerunWritesTheSameRows()
    {
        var token = TestContext.Current.CancellationToken;
        var (organizationId, admin, campaignId) = await SendCampaignAsync(token);
        var contacts = await ReadContactIdsAsync(campaignId, token);
        var sentAt = await ReadStartedAtAsync(campaignId, token);

        // The window runs 14 days after the send, the send instant itself excluded. Only the two orders inside it convert.
        await InsertOrderAsync(organizationId, contacts[0], sentAt - OneHour, token);
        await InsertOrderAsync(organizationId, contacts[1], sentAt + OneDay, token);
        await InsertOrderAsync(organizationId, contacts[2], sentAt + FifteenDays, token);
        await InsertOrderAsync(organizationId, contacts[3], sentAt + OneDay + OneDay, token);
        var expectedConverted = new[] { contacts[1], contacts[3] }.Order().ToArray();

        await RunAttributionAsync(organizationId, campaignId, token);
        var firstRun = await ReadOutcomesAsync(campaignId, token);

        firstRun.Should().HaveCount(ContactCount, "every targeted recipient of both arms has one outcome row");
        firstRun.Where(row => row.Converted).Select(row => row.ContactId).Order().Should().Equal(expectedConverted);

        var results = await GetResultsAsync(admin, campaignId, token);
        var treatmentConverted = results.GetProperty(TreatmentProperty).GetProperty(ConvertedProperty).GetInt64();
        var holdoutConverted = results.GetProperty(HoldoutProperty).GetProperty(ConvertedProperty).GetInt64();
        (treatmentConverted + holdoutConverted).Should().Be(expectedConverted.Length);

        await RunAttributionAsync(organizationId, campaignId, token);
        var secondRun = await ReadOutcomesAsync(campaignId, token);
        secondRun.Should().BeEquivalentTo(firstRun, options => options.Excluding(row => row.EvaluatedAt));
        (await CountResultRowsAsync(campaignId, token)).Should().Be(1, "the result is one row per campaign");
    }

    [Fact]
    public async Task ClosedWindow_WithNoDifference_IsNoMeasurableEffect()
    {
        var token = TestContext.Current.CancellationToken;
        var (organizationId, admin, campaignId) = await SendCampaignAsync(token);
        var contacts = await ReadContactIdsAsync(campaignId, token);
        var sentAt = await ReadStartedAtAsync(campaignId, token);

        // Every contact converts in both arms, so the lift is exactly zero whatever the assignment.
        foreach (var contactId in contacts)
        {
            await InsertOrderAsync(organizationId, contactId, sentAt + OneDay, token);
        }

        _factory.Clock.Advance(ClosedWindowAdvance);
        await RunAttributionAsync(organizationId, campaignId, token);

        var results = await GetResultsAsync(admin, campaignId, token);
        results.GetProperty(WindowStateProperty).GetString().Should().Be(WindowClosed);
        results.GetProperty(IsFinalProperty).GetBoolean().Should().BeTrue();
        results.GetProperty(VerdictProperty).GetString().Should().Be(VerdictNoMeasurableEffect);
        results.GetProperty(LiftProperty).GetDouble().Should().Be(0d);
    }

    /// <summary>
    /// Figures stored while the window is open must not turn into a final claim when the clock passes the window end: until
    /// a run computes them after the end, the result is awaiting its final figures, and no verdict is claimed.
    /// </summary>
    [Fact]
    public async Task ProvisionalFigures_DoNotBecomeFinal_UntilTheWindowEndRecompute()
    {
        var token = TestContext.Current.CancellationToken;
        var (organizationId, admin, campaignId) = await SendCampaignAsync(token);
        var contacts = await ReadContactIdsAsync(campaignId, token);
        var sentAt = await ReadStartedAtAsync(campaignId, token);
        foreach (var contactId in contacts)
        {
            await InsertOrderAsync(organizationId, contactId, sentAt + OneDay, token);
        }

        // Day six of a fourteen-day window: the partial figures are stored while the window is still open.
        _factory.Clock.Advance(DaySixOfFourteen);
        await RunAttributionAsync(organizationId, campaignId, token);

        // The window then ends with no run after it. The clock says closed, the figures say provisional.
        _factory.Clock.Advance(ClosedWindowAdvance);
        var awaiting = await GetResultsAsync(admin, campaignId, token);
        awaiting.GetProperty(WindowStateProperty).GetString().Should().Be(WindowAwaitingFinal);
        awaiting.GetProperty(IsFinalProperty).GetBoolean().Should().BeFalse();
        awaiting.GetProperty(VerdictProperty).GetString().Should().Be(VerdictTooEarly);
        awaiting.GetProperty(VerdictProperty).GetString().Should().NotBe(VerdictMeasurableLift);

        await RunAttributionAsync(organizationId, campaignId, token);
        var final = await GetResultsAsync(admin, campaignId, token);
        final.GetProperty(WindowStateProperty).GetString().Should().Be(WindowClosed);
        final.GetProperty(IsFinalProperty).GetBoolean().Should().BeTrue();
        final.GetProperty(VerdictProperty).GetString().Should().Be(VerdictNoMeasurableEffect);
    }

    [Fact]
    public async Task Results_NeedTheReadPermission_AndRefresh_NeedsTheManagePermission()
    {
        var token = TestContext.Current.CancellationToken;
        var (organizationId, admin, campaignId) = await SendCampaignAsync(token);

        var viewer = await CrmApi.AddMemberAsync(_factory, organizationId, SystemRoleSeed.ViewerRoleId, token);
        using var viewerClient = CrmApi.Client(_factory, viewer.AccessToken);
        var viewerRead = await viewerClient.GetAsync(IdentityApi.Route(CampaignsRoutes.CampaignResults, campaignId), token);
        viewerRead.StatusCode.Should().Be(HttpStatusCode.OK, "the viewer role reads campaigns");

        var viewerRefresh = await viewerClient.PostAsync(IdentityApi.Route(CampaignsRoutes.CampaignResultsRefresh, campaignId), null, token);
        viewerRefresh.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the viewer role cannot refresh");
        (await AuthTestClient.ReadJsonAsync(viewerRefresh, token)).GetProperty(ProblemCodeProperty).GetString()
            .Should().Be(IdentityErrorCodes.PermissionDenied);

        var salesRep = await CrmApi.AddMemberAsync(_factory, organizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var salesRepClient = CrmApi.Client(_factory, salesRep.AccessToken);
        var salesRepRead = await salesRepClient.GetAsync(IdentityApi.Route(CampaignsRoutes.CampaignResults, campaignId), token);
        salesRepRead.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the sales rep role has no campaign read");

        var ownerRefresh = await admin.PostAsync(IdentityApi.Route(CampaignsRoutes.CampaignResultsRefresh, campaignId), null, token);
        ownerRefresh.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Recipients_PageFiltersSearchesAndMasksTheAddress_ByRole()
    {
        var token = TestContext.Current.CancellationToken;
        var (organizationId, admin, campaignId) = await SendCampaignAsync(token);
        var contacts = await ReadContactIdsAsync(campaignId, token);
        var sentAt = await ReadStartedAtAsync(campaignId, token);
        await InsertOrderAsync(organizationId, contacts[0], sentAt + OneDay, token);
        await InsertOrderAsync(organizationId, contacts[1], sentAt + OneDay, token);
        await RunAttributionAsync(organizationId, campaignId, token);
        var route = IdentityApi.Route(CampaignsRoutes.CampaignRecipients, campaignId);

        // Paging: eight recipients in pages of three, with the last page short and without a next cursor.
        var seen = new List<string>();
        string? cursor = null;
        for (var page = 0; page < 3; page++)
        {
            var query = "limit=3" + (cursor is null ? string.Empty : "&after=" + Uri.EscapeDataString(cursor));
            var body = await GetRecipientsAsync(admin, route, query, token);
            var items = body.GetProperty(ItemsProperty).EnumerateArray().ToList();
            items.Should().HaveCount(page < 2 ? 3 : 2);
            seen.AddRange(items.Select(item => item.GetProperty(RecipientIdProperty).GetString()!));
            cursor = body.GetProperty(NextCursorProperty).ValueKind == JsonValueKind.Null ? null : body.GetProperty(NextCursorProperty).GetString();
        }

        seen.Should().HaveCount(ContactCount).And.OnlyHaveUniqueItems("each recipient appears once across the pages");
        cursor.Should().BeNull("the last page has no next cursor");

        // Filters: the holdout arm is pinned to be non-empty, so the arm and send-status checks always apply.
        var holdout = (await GetRecipientsAsync(admin, route, "limit=50&arm=holdout", token)).GetProperty(ItemsProperty).EnumerateArray().ToList();
        holdout.Should().NotBeEmpty();
        holdout.Should().OnlyContain(item => item.GetProperty(ArmProperty).GetString() == HoldoutArm);
        holdout.Should().OnlyContain(item => item.GetProperty(SendStatusProperty).ValueKind == JsonValueKind.Null);

        var sent = (await GetRecipientsAsync(admin, route, "limit=50&sendStatus=sent", token)).GetProperty(ItemsProperty).EnumerateArray().ToList();
        sent.Should().HaveCount(ContactCount - holdout.Count, "only treatment recipients were emailed");

        var converted = (await GetRecipientsAsync(admin, route, "limit=50&converted=true", token)).GetProperty(ItemsProperty).EnumerateArray().ToList();
        converted.Select(item => item.GetProperty(ContactIdProperty).GetGuid()).Should().BeEquivalentTo(new[] { contacts[0], contacts[1] });
        converted.Should().OnlyContain(item => item.GetProperty(ConversionProperty).GetString() == CampaignRecipientConversions.Converted);

        var named = (await GetRecipientsAsync(admin, route, "limit=50&q=attribution3", token)).GetProperty(ItemsProperty).EnumerateArray().ToList();
        named.Should().ContainSingle();
        named[0].GetProperty(ContactNameProperty).GetString().Should().Contain("attribution3");

        var refused = await admin.GetAsync(route + "?arm=elsewhere", token);
        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // The address: visible to the owner, whose contacts scope is the whole organization, and hidden for the viewer role.
        holdout[0].GetProperty(EmailVisibleProperty).GetBoolean().Should().BeTrue();
        holdout[0].GetProperty(EmailProperty).ValueKind.Should().Be(JsonValueKind.String);

        var viewer = await CrmApi.AddMemberAsync(_factory, organizationId, SystemRoleSeed.ViewerRoleId, token);
        using var viewerClient = CrmApi.Client(_factory, viewer.AccessToken);
        var viewerRows = (await GetRecipientsAsync(viewerClient, route, "limit=50", token)).GetProperty(ItemsProperty).EnumerateArray().ToList();
        viewerRows.Should().HaveCount(ContactCount);
        viewerRows.Should().OnlyContain(item => !item.GetProperty(EmailVisibleProperty).GetBoolean());
        viewerRows.Should().OnlyContain(item => item.GetProperty(EmailProperty).ValueKind == JsonValueKind.Null);

        var salesRep = await CrmApi.AddMemberAsync(_factory, organizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var salesRepClient = CrmApi.Client(_factory, salesRep.AccessToken);
        var salesRepRead = await salesRepClient.GetAsync(route, token);
        salesRepRead.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the sales rep role has no campaign read");
    }

    private static async Task<JsonElement> GetRecipientsAsync(HttpClient client, string route, string query, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(route + "?" + query, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    /// <summary>Creates a template and eight contacts, prepares and starts one campaign with a 10 % holdout, and sends it.</summary>
    private async Task<(Guid OrganizationId, HttpClient Admin, Guid CampaignId)> SendCampaignAsync(CancellationToken cancellationToken)
    {
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, cancellationToken);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, cancellationToken);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"attribution{index}"), cancellationToken);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(
            admin,
            templateId,
            HoldoutShareBps,
            CampaignTargetingModes.All,
            null,
            cancellationToken);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, cancellationToken, MinimumHoldout, MinimumTreatment);

        var start = await CampaignSendHarness.StartAsync(admin, campaignId, IdempotencyKey, cancellationToken);
        start.IsSuccessStatusCode.Should().BeTrue();
        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, cancellationToken);
        return (owner.OrganizationId, admin, campaignId);
    }

    private async Task RunAttributionAsync(Guid organizationId, Guid campaignId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CampaignAttributionJob>().ExecuteAsync(organizationId, campaignId, cancellationToken);
    }

    private static async Task<JsonElement> GetResultsAsync(HttpClient client, Guid campaignId, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(IdentityApi.Route(CampaignsRoutes.CampaignResults, campaignId), cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    /// <summary>The contacts of the campaign, in contact id order.</summary>
    private async Task<List<Guid>> ReadContactIdsAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var rows = await CampaignSendHarness.ReadRecipientsAsync(_connectionString, campaignId, cancellationToken);
        return rows.Select(row => row.ContactId).Order().ToList();
    }

    private async Task<DateTimeOffset> ReadStartedAtAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(CampaignStartedAtSql, connection);
        command.Parameters.AddWithValue(ParameterCampaign, campaignId);
        var value = (DateTime)(await command.ExecuteScalarAsync(cancellationToken))!;
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private async Task InsertOrderAsync(Guid organizationId, Guid contactId, DateTimeOffset orderedAt, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO crm.orders
                (id, organization_id, contact_id, external_ref, ordered_at, total_amount, currency, line_count,
                 is_cancellation, source, created_at, updated_at)
            VALUES (gen_random_uuid(), @org, @contact, @reference, @ordered, @amount, @currency, 1, false, @source, @ordered, @ordered)
            """,
            connection);
        command.Parameters.AddWithValue(ParameterOrganization, organizationId);
        command.Parameters.AddWithValue(ParameterContact, contactId);
        command.Parameters.AddWithValue(ParameterReference, $"attribution-{contactId:N}-{orderedAt.ToUnixTimeSeconds()}");
        command.Parameters.AddWithValue(ParameterOrderedAt, orderedAt);
        command.Parameters.AddWithValue(ParameterAmount, OrderAmount);
        command.Parameters.AddWithValue(ParameterCurrency, CrmDemoContract.ExportCurrency);
        command.Parameters.AddWithValue(ParameterSource, OrderSources.Manual);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<List<OutcomeRow>> ReadOutcomesAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT r.contact_id, o.converted, o.revenue, o.orders_in_window, o.simulated_orders_in_window, o.evaluated_at
            FROM campaigns.recipient_outcomes o
            JOIN campaigns.recipients r ON r.id = o.recipient_id
            WHERE o.campaign_id = @campaign
            ORDER BY r.contact_id
            """,
            connection);
        command.Parameters.AddWithValue(ParameterCampaign, campaignId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<OutcomeRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new OutcomeRow(
                reader.GetGuid(0),
                reader.GetBoolean(1),
                reader.GetDecimal(2),
                reader.GetInt32(3),
                reader.GetInt32(4),
                reader.GetDateTime(5)));
        }

        return rows;
    }

    private async Task<long> CountResultRowsAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM campaigns.campaign_results WHERE campaign_id = @campaign", connection);
        command.Parameters.AddWithValue(ParameterCampaign, campaignId);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private sealed record OutcomeRow(
        Guid ContactId,
        bool Converted,
        decimal Revenue,
        int OrdersInWindow,
        int SimulatedOrdersInWindow,
        DateTime EvaluatedAt);
}
