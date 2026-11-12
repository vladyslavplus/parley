using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Api.Controllers.Campaigns;
using Parley.Api.Controllers.Crm;
using Parley.Api.Controllers.Dashboards;
using Parley.Api.Controllers.Insights;
using Parley.Campaigns.Contracts.Errors;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Orders;
using Parley.Crm.Shared;
using Parley.Identity.Authorization.Seed;
using Parley.Insights.Contracts.Errors;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Features.Daily;
using Parley.Insights.Shared;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Insights;

/// <summary>
/// The RFM snapshot, the priority list, the ML switch and the dashboard, end to end on the API host. Orders are inserted
/// by SQL with fixed ages, so each contact has a known segment: the active one needs attention, the lapsed one is lost and
/// the middle one is regular. The clock of the host is fixed, so the snapshot day does not move during a test.
/// </summary>
public sealed class InsightsPriorityIntegrationTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ActiveName = "Active";

    private const string LapsedName = "Lapsed";

    private const string MiddleName = "Middle";

    private const decimal OrderAmount = 100m;

    private const string SnapshotsSql = "SELECT contact_id, segment FROM insights.rfm_snapshots WHERE organization_id = @org ORDER BY contact_id";

    private const string PrioritySql = "SELECT contact_id, rank, priority_value FROM insights.priority_items WHERE organization_id = @org ORDER BY contact_id";

    private const string AuditSql = "SELECT count(*) FROM insights.outbox_messages WHERE payload::text LIKE @pattern";

    private const string ParameterOrganization = "org";

    private const string ParameterPattern = "pattern";

    private const string ParameterContact = "contact";

    private const string ParameterReference = "reference";

    private const string ParameterOrderedAt = "ordered";

    private const string ParameterAmount = "amount";

    private const string ParameterCurrency = "currency";

    private const string ParameterSource = "source";

    private const string IdProperty = "id";

    private const string ParameterDay = "day";

    private const string ParameterRfm = "rfm";

    private const string ParameterPriority = "priority";

    private const string ParameterOwner = "owner";

    private const string ParameterCount = "count";

    private const string PartialSnapshotSql = """
        INSERT INTO insights.rfm_snapshots
            (id, organization_id, created_at, updated_at, contact_id, owner_user_id, as_of, recency_days, frequency, monetary,
             r_tier, f_tier, m_tier, segment)
        SELECT gen_random_uuid(), @org, now(), now(), c.id, NULL, @day, 1, 1, 1, 5, 5, 5, 'champions'
        FROM crm.contacts c
        WHERE c.organization_id = @org AND c.deleted_at IS NULL
        """;

    private const string InsertDailyRunSql = """
        INSERT INTO insights.daily_runs (id, organization_id, created_at, updated_at, as_of, rfm_completed_at, priority_completed_at)
        VALUES (gen_random_uuid(), @org, now(), now(), @day, CASE WHEN @rfm THEN now() END, CASE WHEN @priority THEN now() END)
        """;

    private const string StampPrioritySql = """
        UPDATE insights.daily_runs SET priority_completed_at = now() WHERE organization_id = @org AND as_of = @day
        """;

    private const string DailyRunCompleteSql = """
        SELECT bool_and(rfm_completed_at IS NOT NULL AND priority_completed_at IS NOT NULL)
        FROM insights.daily_runs WHERE organization_id = @org AND as_of = @day
        """;

    private const string DailyJobCountSql =
        "SELECT count(*) FROM hangfire.job WHERE invocationdata::text LIKE '%InsightsOrganizationDailyJob%'";

    private const string BulkSegmentRowsSql = """
        INSERT INTO insights.rfm_snapshots
            (id, organization_id, created_at, updated_at, contact_id, owner_user_id, as_of, recency_days, frequency, monetary,
             r_tier, f_tier, m_tier, segment)
        SELECT gen_random_uuid(), @org, now(), now(), gen_random_uuid(), NULL, @day, NULL, 0, 0, NULL, NULL, NULL, 'regular'
        FROM generate_series(1, @count)
        """;

    private const string OwnedSegmentRowSql = """
        INSERT INTO insights.rfm_snapshots
            (id, organization_id, created_at, updated_at, contact_id, owner_user_id, as_of, recency_days, frequency, monetary,
             r_tier, f_tier, m_tier, segment)
        VALUES (gen_random_uuid(), @org, now(), now(), @contact, @owner, @day, NULL, 0, 0, NULL, NULL, NULL, 'regular')
        """;

    private const string ItemsProperty = "items";

    private const string ContactIdProperty = "contactId";

    private const string VersionProperty = "version";

    private const string MlEnabledProperty = "mlEnabled";

    private const string CountProperty = "count";

    private const string StagesProperty = "stages";

    private const string OpenDealsProperty = "openDeals";

    private const string TasksDueProperty = "tasksDue";

    private const string FirstNameProperty = "firstName";

    private const string SegmentProperty = "segment";

    private const string ContactLastName = "Contact";

    private const string DefaultCountry = "DE";

    private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);

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
    public async Task RebuildOfTheSameDay_WritesTheSameRows()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SeedSegmentsAsync(owner, admin, withEmail: false, token);

        await RunDailyJobAsync(owner.OrganizationId, token);
        var firstSnapshots = await ReadSnapshotsAsync(owner.OrganizationId, token);
        var firstPriority = await ReadPriorityAsync(owner.OrganizationId, token);

        await RunDailyJobAsync(owner.OrganizationId, token);

        firstSnapshots.Select(row => row.Segment).Order().Should().Equal("lost", "needs_attention", "regular");
        firstPriority.Should().HaveCount(3, "every contact with a positive segment weight is on the list");
        (await ReadSnapshotsAsync(owner.OrganizationId, token)).Should().Equal(firstSnapshots);
        (await ReadPriorityAsync(owner.OrganizationId, token)).Should().Equal(firstPriority);
    }

    [Fact]
    public async Task Representative_SeesOnlyTheContactsTheyOwn()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        var representative = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var ownedContact = await CreateContactAsync(admin, "Owned", representative.UserId, token);
        var unownedContact = await CreateContactAsync(admin, "Shared", null, token);
        var now = _factory.Clock.GetUtcNow();
        await InsertOrderAsync(owner.OrganizationId, ownedContact, now - FiveDays, token);
        await InsertOrderAsync(owner.OrganizationId, unownedContact, now - FiveDays, token);
        await RunDailyJobAsync(owner.OrganizationId, token);

        var representativeClient = CrmApi.Client(_factory, representative.AccessToken);
        (await ReadPriorityContactIdsAsync(representativeClient, token)).Should().Equal(ownedContact);
        (await ReadPriorityContactIdsAsync(admin, token)).Should().BeEquivalentTo(new[] { ownedContact, unownedContact });
    }

    [Fact]
    public async Task Organizations_DoNotSeeEachOthersPriority()
    {
        var token = TestContext.Current.CancellationToken;
        var first = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var second = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var firstAdmin = CrmApi.Client(_factory, first.AccessToken);
        var secondAdmin = CrmApi.Client(_factory, second.AccessToken);
        var firstContact = await CreateContactAsync(firstAdmin, "First", null, token);
        var secondContact = await CreateContactAsync(secondAdmin, "Second", null, token);
        var now = _factory.Clock.GetUtcNow();
        await InsertOrderAsync(first.OrganizationId, firstContact, now - FiveDays, token);
        await InsertOrderAsync(second.OrganizationId, secondContact, now - FiveDays, token);

        await RunDailyJobAsync(first.OrganizationId, token);
        await RunDailyJobAsync(second.OrganizationId, token);

        (await ReadPriorityContactIdsAsync(firstAdmin, token)).Should().Equal(firstContact);
        (await ReadSnapshotsAsync(second.OrganizationId, token)).Should().HaveCount(1);
    }

    [Fact]
    public async Task MlSwitch_NeedsTheManagePermission_AndIsAudited()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        var viewer = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.ViewerRoleId, token);
        var viewerClient = CrmApi.Client(_factory, viewer.AccessToken);

        var denied = await viewerClient.PutAsJsonAsync(InsightsRoutes.MlSettings, new { mlEnabled = true, version = 0 }, token);
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var current = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.MlSettings, token), token);
        current.GetProperty(MlEnabledProperty).GetBoolean().Should().BeFalse("ML is off until an administrator switches it on");
        var version = current.GetProperty(VersionProperty).GetUInt32();

        var changed = await admin.PutAsJsonAsync(InsightsRoutes.MlSettings, new { mlEnabled = true, version }, token);
        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AuthTestClient.ReadJsonAsync(changed, token)).GetProperty(MlEnabledProperty).GetBoolean().Should().BeTrue();

        var stale = await admin.PutAsJsonAsync(InsightsRoutes.MlSettings, new { mlEnabled = false, version }, token);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(stale, token)).Should().Be(InsightsErrorCodes.MlSettingsVersionConflict);

        (await CountAuditRowsAsync("%insights.ml_settings.changed%", token)).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Dashboard_IsAvailableToTheViewer_AndCarriesTotalsOnly()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        var viewer = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.ViewerRoleId, token);

        var overview = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(DashboardsRoutes.Overview, token), token);
        overview.GetProperty(StagesProperty).GetArrayLength().Should().Be(0, "no deal is open yet");
        overview.GetProperty(OpenDealsProperty).GetInt32().Should().Be(0);
        overview.GetProperty(TasksDueProperty).GetInt32().Should().Be(0);

        var viewerResponse = await CrmApi.Client(_factory, viewer.AccessToken).GetAsync(DashboardsRoutes.Overview, token);
        viewerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CampaignsRfmFilter_CountsTheSnapshotSegment_AndRefusesBeforeTheSnapshotExists()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SeedSegmentsAsync(owner, admin, withEmail: true, token);

        var before = await admin.PostAsJsonAsync(CampaignsRoutes.SegmentPreview, new { rfmSegments = new[] { "needs_attention" } }, token);
        before.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.ErrorCodeAsync(before, token)).Should().Be(CampaignsErrorCodes.SegmentRfmNotAvailable);

        await RunDailyJobAsync(owner.OrganizationId, token);

        (await PreviewCountAsync(admin, "needs_attention", token)).Should().Be(1);
        (await PreviewCountAsync(admin, "lost", token)).Should().Be(1);
        (await PreviewCountAsync(admin, "champions", token)).Should().Be(0);
    }

    [Fact]
    public async Task ContactList_CarriesTheSegment_AndFiltersBySegment()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SeedSegmentsAsync(owner, admin, withEmail: false, token);

        (await ReadContactListAsync(admin, string.Empty, token)).Select(item => item.Segment).Should().AllSatisfy(
            segment => segment.Should().BeNull("no snapshot exists before the daily job runs"));

        await RunDailyJobAsync(owner.OrganizationId, token);

        (await ReadContactListAsync(admin, string.Empty, token)).Should().BeEquivalentTo(new[]
        {
            (FirstName: ActiveName, Segment: (string?)"needs_attention"),
            (FirstName: LapsedName, Segment: (string?)"lost"),
            (FirstName: MiddleName, Segment: (string?)"regular"),
        });
        (await ReadContactListAsync(admin, "?segment=lost", token)).Select(item => item.FirstName).Should().Equal(LapsedName);

        var unknown = await admin.GetAsync($"{CrmRoutes.Contacts}?segment=Lost", token);
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PartialSnapshot_IsInvisibleToTheReaders_UntilItsDayIsComplete()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SeedSegmentsAsync(owner, admin, withEmail: true, token);

        // A day still being written: its rows exist, its completion stamp does not.
        await ExecuteAsync(PartialSnapshotSql, (ParameterOrganization, owner.OrganizationId), (ParameterDay, TodayOf(_factory)));

        (await ReadContactListAsync(admin, string.Empty, token)).Select(item => item.Segment).Should().AllSatisfy(
            segment => segment.Should().BeNull("a day without a completion stamp is not visible"));
        var preview = await admin.PostAsJsonAsync(CampaignsRoutes.SegmentPreview, new { rfmSegments = new[] { RfmSegmentNames.Champions } }, token);
        preview.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.ErrorCodeAsync(preview, token)).Should().Be(CampaignsErrorCodes.SegmentRfmNotAvailable);
    }

    [Fact]
    public async Task DayWhosePriorityStepFailed_IsQueuedAgainBySweep_UntilComplete()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SeedSegmentsAsync(owner, admin, withEmail: false, token);
        var today = TodayOf(_factory);

        // The RFM step finished; the priority step did not.
        await ExecuteAsync(InsertDailyRunSql, (ParameterOrganization, owner.OrganizationId), (ParameterDay, today), (ParameterRfm, true), (ParameterPriority, false));
        var queuedBefore = await CountDailyJobsAsync(token);
        await RunSweepAsync(token);
        (await CountDailyJobsAsync(token)).Should().Be(queuedBefore + 1, "the day is incomplete, so the sweep queues it again");

        // With both stamps set, the sweep leaves the day alone.
        await ExecuteAsync(StampPrioritySql, (ParameterOrganization, owner.OrganizationId), (ParameterDay, today));
        var queuedComplete = await CountDailyJobsAsync(token);
        await RunSweepAsync(token);
        (await CountDailyJobsAsync(token)).Should().Be(queuedComplete);
    }

    [Fact]
    public async Task ConcurrentRunsOfOneDay_LeaveACompleteSnapshot()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SeedSegmentsAsync(owner, admin, withEmail: false, token);

        // The day lock lets one run through; the other leaves at once. Either way the day ends complete and whole.
        await Task.WhenAll(RunDailyJobAsync(owner.OrganizationId, token), RunDailyJobAsync(owner.OrganizationId, token));

        (await ReadSnapshotsAsync(owner.OrganizationId, token)).Should().HaveCount(3);
        (await ScalarAsync<bool>(
            DailyRunCompleteSql,
            [(ParameterOrganization, owner.OrganizationId), (ParameterDay, TodayOf(_factory))],
            token)).Should().BeTrue("both steps stamped the day");
    }

    [Fact]
    public async Task SegmentFilter_CountsOnlyTheCallersContacts_WhenTheOrganizationSegmentIsAboveTheCap()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        var representative = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var ownedContact = await CreateContactAsync(admin, "Owned", representative.UserId, token);
        var today = TodayOf(_factory);

        // Above the cap for the organization, but none of it is owned by the representative.
        await ExecuteAsync(
            BulkSegmentRowsSql,
            (ParameterOrganization, owner.OrganizationId),
            (ParameterDay, today),
            (ParameterCount, CrmLimits.ContactSegmentFilterMaximum + 1));
        await ExecuteAsync(
            OwnedSegmentRowSql,
            (ParameterOrganization, owner.OrganizationId),
            (ParameterDay, today),
            (ParameterContact, ownedContact),
            (ParameterOwner, representative.UserId));
        await ExecuteAsync(InsertDailyRunSql, (ParameterOrganization, owner.OrganizationId), (ParameterDay, today), (ParameterRfm, true), (ParameterPriority, true));

        var segmentQuery = $"{CrmRoutes.Contacts}?segment={RfmSegmentNames.Regular}";
        var representativeResponse = await CrmApi.Client(_factory, representative.AccessToken).GetAsync(segmentQuery, token);
        representativeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(representativeResponse, token);
        page.GetProperty(ItemsProperty).EnumerateArray().Select(item => item.GetProperty(IdProperty).GetGuid())
            .Should().Equal(ownedContact);

        var organizationResponse = await admin.GetAsync(segmentQuery, token);
        organizationResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(organizationResponse, "segment", token)).Should().Be(CrmErrorCodes.ContactSegmentTooLarge);
    }

    /// <summary>Creates three contacts with three known segments, each with its own orders. Emails are added when asked for.</summary>
    private async Task SeedSegmentsAsync(OrganizationOwner owner, HttpClient admin, bool withEmail, CancellationToken cancellationToken)
    {
        var now = _factory.Clock.GetUtcNow();
        var active = await CreateContactAsync(admin, ActiveName, null, cancellationToken, withEmail);
        var lapsed = await CreateContactAsync(admin, LapsedName, null, cancellationToken, withEmail);
        var middle = await CreateContactAsync(admin, MiddleName, null, cancellationToken, withEmail);

        // Active: three orders in the last three days. Middle: two orders, last one ten days ago. Lapsed: one order, 100 days ago.
        for (var day = 1; day <= 3; day++)
        {
            await InsertOrderAsync(owner.OrganizationId, active, now - TimeSpan.FromDays(day), cancellationToken);
        }

        await InsertOrderAsync(owner.OrganizationId, middle, now - TimeSpan.FromDays(10), cancellationToken, amount: OrderAmount / 2);
        await InsertOrderAsync(owner.OrganizationId, middle, now - TimeSpan.FromDays(11), cancellationToken, amount: OrderAmount / 2);
        await InsertOrderAsync(owner.OrganizationId, lapsed, now - TimeSpan.FromDays(100), cancellationToken, amount: OrderAmount / 2);
    }

    private async Task<Guid> CreateContactAsync(
        HttpClient admin,
        string name,
        Guid? ownerUserId,
        CancellationToken cancellationToken,
        bool withEmail = false)
    {
        var body = withEmail
            ? CampaignSendHarness.ContactAt(name)
            : new ContactBody(name, ContactLastName, null, null, null, DefaultCountry, null, null, ownerUserId, null);
        var created = await CrmApi.CreateContactAsync(admin, body, cancellationToken);
        return created.GetProperty("id").GetGuid();
    }

    private async Task RunDailyJobAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(_factory.Clock.GetUtcNow().UtcDateTime).ToString(InsightsLimits.DayFormat, CultureInfo.InvariantCulture);
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<InsightsOrganizationDailyJob>().ExecuteAsync(organizationId, day, cancellationToken);
    }

    private async Task InsertOrderAsync(
        Guid organizationId,
        Guid contactId,
        DateTimeOffset orderedAt,
        CancellationToken cancellationToken,
        decimal amount = OrderAmount)
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
        command.Parameters.AddWithValue(ParameterReference, $"insights-{Guid.NewGuid():N}");
        command.Parameters.AddWithValue(ParameterOrderedAt, orderedAt);
        command.Parameters.AddWithValue(ParameterAmount, amount);
        command.Parameters.AddWithValue(ParameterCurrency, CrmDemoContract.ExportCurrency);
        command.Parameters.AddWithValue(ParameterSource, OrderSources.Manual);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<List<(Guid ContactId, string Segment)>> ReadSnapshotsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SnapshotsSql, connection);
        command.Parameters.AddWithValue(ParameterOrganization, organizationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<(Guid ContactId, string Segment)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetGuid(0), reader.GetString(1)));
        }

        return rows;
    }

    private async Task<List<(Guid ContactId, int Rank, decimal Value)>> ReadPriorityAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(PrioritySql, connection);
        command.Parameters.AddWithValue(ParameterOrganization, organizationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<(Guid ContactId, int Rank, decimal Value)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetGuid(0), reader.GetInt32(1), reader.GetDecimal(2)));
        }

        return rows;
    }

    private async Task<long> CountAuditRowsAsync(string pattern, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(AuditSql, connection);
        command.Parameters.AddWithValue(ParameterPattern, pattern);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<List<Guid>> ReadPriorityContactIdsAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{InsightsRoutes.Priority}?limit=50", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty).EnumerateArray()
            .Select(item => item.GetProperty(ContactIdProperty).GetGuid())
            .Order()
            .ToList();
    }

    private static DateOnly TodayOf(IdentityApiFactory factory) => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<InsightsDailySweepJob>().ExecuteAsync(null, cancellationToken);
    }

    private async Task<long> CountDailyJobsAsync(CancellationToken cancellationToken) =>
        await ScalarAsync<long>(DailyJobCountSql, [], cancellationToken);

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private async Task<T> ScalarAsync<T>(string sql, (string Name, object Value)[] parameters, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return (T)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task<int> PreviewCountAsync(HttpClient admin, string segment, CancellationToken cancellationToken)
    {
        var response = await admin.PostAsJsonAsync(CampaignsRoutes.SegmentPreview, new { rfmSegments = new[] { segment } }, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await AuthTestClient.ReadJsonAsync(response, cancellationToken)).GetProperty(CountProperty).GetInt32();
    }

    private static async Task<List<(string FirstName, string? Segment)>> ReadContactListAsync(
        HttpClient client,
        string query,
        CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{CrmRoutes.Contacts}{query}", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty).EnumerateArray()
            .Select(item => (
                FirstName: item.GetProperty(FirstNameProperty).GetString()!,
                Segment: item.GetProperty(SegmentProperty).GetString()))
            .ToList();
    }
}
