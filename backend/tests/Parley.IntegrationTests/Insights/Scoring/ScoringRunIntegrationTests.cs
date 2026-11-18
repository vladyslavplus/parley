using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Api.Controllers.Insights;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Domain.Orders;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Domain.Organizations;
using Parley.Insights.Contracts.Errors;
using Parley.Insights.Contracts.Priority;
using Parley.Insights.Contracts.Rfm;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Features.Daily;
using Parley.Insights.Features.Priority.Rebuild;
using Parley.Insights.Features.Scoring.Client;
using Parley.Insights.Features.Scoring.Models;
using Parley.Insights.Features.Scoring.Run;
using Parley.Insights.Shared;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Insights.Scoring.Support;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Insights.Scoring;

/// <summary>
/// The scoring integration on the API host, with the scoring service replaced by <see cref="FakeScoringService"/>: a run is
/// queued by the API, its job is run in-process, and the scores, the failure rules, the permissions and the priority ML
/// mode are read back through the API. The batch size is 2, so every run with three scored contacts takes two requests.
/// </summary>
public sealed class ScoringRunIntegrationTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ApiKey = "test-key";

    private const string BatchSizeKey = "Insights:Scoring:BatchSize";

    private const string ApiKeyConfigurationKey = "Insights:Scoring:ApiKey";

    private const string BatchSizeValue = "2";

    private const string UpliftKind = "uplift";

    private const int TestHoldoutShareBps = 5000;

    private const int TestTargetingShareBps = 5000;

    private const string CountSql = "SELECT count(*) FROM insights.latest_scores WHERE organization_id = @org AND score_kind = 'uplift'";

    private const string LatestIdsSql = "SELECT score_id FROM insights.latest_scores WHERE organization_id = @org AND score_kind = 'uplift' ORDER BY score_id";

    private const string UnnamedScoresSql = "SELECT count(*) FROM insights.scores WHERE organization_id = @org AND (model_name = '' OR model_version = '')";

    private const string ScoresOfRunSql = "SELECT count(*) FROM insights.scores WHERE organization_id = @org AND scoring_run_id = @run";

    private const string ParameterOrganization = "org";

    private const string ParameterContact = "contact";

    private const string ParameterReference = "reference";

    private const string ParameterOrderedAt = "ordered";

    private const string ParameterAmount = "amount";

    private const string ParameterCurrency = "currency";

    private const string ParameterSource = "source";

    private const string ContactLastName = "Contact";

    private const string DefaultCountry = "DE";

    private const string StatusProperty = "status";

    private const string ErrorCodeProperty = "errorCode";

    private const string ScoresProperty = "scores";

    private const string ValueProperty = "value";

    private const string SegmentProperty = "segment";

    private const string UpliftSegmentProperty = "upliftSegment";

    private const string UpliftValueProperty = "upliftValue";

    private const string PTreatedProperty = "pTreated";

    private const string PControlProperty = "pControl";

    private const string PAliveProperty = "pAlive";

    private const string ExpectedPurchasesProperty = "expectedPurchases";

    private const string ExpectedAverageValueProperty = "expectedAverageValue";

    private const string HorizonDaysProperty = "horizonDays";

    private const string CurrencyProperty = "currency";

    private const string SegmentRulesProperty = "segmentRules";

    private const string ContactsSkippedProperty = "contactsSkipped";

    private const string PayloadProperty = "payload";

    private const string ModelVersionProperty = "modelVersion";

    private const string ModelsProperty = "models";

    private const string IsActiveProperty = "isActive";

    private const string IsAvailableProperty = "isAvailable";

    private const string EffectiveUpliftVersionProperty = "effectiveUpliftVersion";

    private const string ItemsProperty = "items";

    private const string ContactIdProperty = "contactId";

    private const string ModeProperty = "mode";

    private const string MlStatusProperty = "mlStatus";

    private const string ReasonCodesProperty = "reasonCodes";

    private const string CodeProperty = "code";

    private const string IdProperty = "id";

    private const string Persuadable = "persuadable";

    private const string DoNotDisturbSegment = "do_not_disturb";

    private const string ReasonAtRisk = "reason.at_risk";

    private const string ReasonPersuadable = "reason.persuadable";

    private const string ReasonUpliftHigh = "reason.uplift_high";

    private const string ReasonDoNotDisturb = "reason.do_not_disturb";

    private const string ModelProvenanceProperty = "modelProvenance";
    private const string UpliftHighThresholdProperty = "upliftHighThreshold";
    private const decimal ConfiguredUpliftHighThreshold = 0.2m;
    private const string StaleDealDaysProperty = "staleDealDays";
    private const int ConfiguredStaleDealDays = 14;

    private const string RunSucceeded = "completed";

    private const string RunFailed = "failed";

    private const string ClvPayload =
        """{"p_alive":0.91,"expected_purchases":6.5,"expected_avg_value":120.25,"horizon_days":365}""";

    private static readonly TimeSpan ThirtyDays = TimeSpan.FromDays(30);

    private readonly FakeScoringService _scoring = new();

    private IdentityApiFactory _factory = null!;

    private string _connectionString = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _connectionString = postgres.ConnectionString;
        _factory = new IdentityApiFactory(
            postgres,
            mailpit,
            extraConfiguration: new Dictionary<string, string?>
            {
                [ApiKeyConfigurationKey] = ApiKey,
                [BatchSizeKey] = BatchSizeValue,
            },
            extraServices: services => services.AddHttpClient<ScoringServiceClient>()
                .ConfigurePrimaryHttpMessageHandler(() => _scoring));
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task UpliftRun_ScoresEveryContactWithOrders_InBatches_AndPinsTheSyncedModel()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        await CreateOrderedContactsAsync(admin, owner.OrganizationId, 3, null, token);
        await CreateContactAsync(admin, "NoOrders", null, token);

        var runId = await StartRunAsync(admin, UpliftKind, token);
        await RunJobAsync(owner.OrganizationId, runId, token);

        var run = await ReadRunAsync(admin, runId, token);
        run.GetProperty(StatusProperty).GetString().Should().Be(RunSucceeded);
        run.GetProperty("contactsScored").GetInt32().Should().Be(3, "the contact without an order has no features");
        run.GetProperty("contactsRejected").GetInt32().Should().Be(0);
        run.GetProperty(ContactsSkippedProperty).GetInt32().Should().Be(1, "the contact without orders is counted as skipped");
        run.GetProperty(ModelVersionProperty).GetString().Should().Be(FakeScoringService.ModelVersion);
        _scoring.UpliftBatchSizes.Should().HaveCount(2, "three contacts in batches of two");
        _scoring.UpliftBatchSizes.Sum().Should().Be(3);
        _scoring.RequestedVersions.Should().OnlyContain(version => version == FakeScoringService.ModelVersion, "the run pins the synced version");
        (await ScalarAsync(CountSql, owner.OrganizationId, token)).Should().Be(3);
        (await ScalarAsync(UnnamedScoresSql, owner.OrganizationId, token)).Should().Be(0, "every score row names its model and version");

        var models = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Models, token), token);
        var active = models.EnumerateArray().Single(model => model.GetProperty("modelName").GetString() == FakeScoringService.ModelName);
        active.GetProperty(IsActiveProperty).GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task FailedRun_LeavesTheLiveScoresAsTheyWere()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        await CreateOrderedContactsAsync(admin, owner.OrganizationId, 3, null, token);
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);
        var liveBefore = await ReadLatestIdsAsync(owner.OrganizationId, token);

        _scoring.RejectAll = true;
        var failed = await StartRunAsync(admin, UpliftKind, token);
        await RunJobAsync(owner.OrganizationId, failed, token);

        var run = await ReadRunAsync(admin, failed, token);
        run.GetProperty(StatusProperty).GetString().Should().Be(RunFailed);
        run.GetProperty(ErrorCodeProperty).GetString().Should().Be(InsightsErrorCodes.ScoringTooManyRejected);
        (await ReadLatestIdsAsync(owner.OrganizationId, token)).Should().Equal(liveBefore, "a failed run swaps nothing in");
    }

    [Fact]
    public async Task Swap_WhenTheRunEndedWhileItWorked_LeavesTheLiveScoresAsTheyWere()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        await CreateOrderedContactsAsync(admin, owner.OrganizationId, 3, null, token);
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);
        var liveBefore = await ReadLatestIdsAsync(owner.OrganizationId, token);

        // A newer contact joins the next run, so a swap would change the live set. While the runner waits on the service,
        // the sweep fails the run; the guarded swap must not publish the partial result or flip the run to completed.
        await CreateOrderedContactsAsync(admin, owner.OrganizationId, 1, null, token);
        var runId = await StartRunAsync(admin, UpliftKind, token);
        _scoring.BeforeUpliftResponseAsync = () => MarkRunFailedAsync(runId, token);

        await RunJobAsync(owner.OrganizationId, runId, token);

        (await ReadRunAsync(admin, runId, token)).GetProperty(StatusProperty).GetString().Should().Be(RunFailed);
        (await ReadLatestIdsAsync(owner.OrganizationId, token)).Should().Equal(liveBefore, "a run that left running swaps nothing in");
    }

    [Fact]
    public async Task FailureOfACompletedRun_DoesNotDeleteItsScores()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        await CreateOrderedContactsAsync(admin, owner.OrganizationId, 3, null, token);
        var runId = await StartRunAsync(admin, UpliftKind, token);
        await RunJobAsync(owner.OrganizationId, runId, token);
        var scoresBefore = await CountScoresOfRunAsync(owner.OrganizationId, runId, token);
        scoresBefore.Should().Be(3);

        // A duplicate worker that was still in flight reaches its failure path after the run completed. The failure must
        // change nothing: the rows of the completed run are what pinned readers read.
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
            var runner = scope.ServiceProvider.GetRequiredService<ScoringRunRunner>();
            var failure = typeof(ScoringRunRunner).GetMethod("FailAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            await ((Task)failure.Invoke(runner, [runId, InsightsErrorCodes.ScoringRunFailed])!).WaitAsync(token);
        }

        (await ReadRunAsync(admin, runId, token)).GetProperty(StatusProperty).GetString().Should().Be(RunSucceeded);
        (await CountScoresOfRunAsync(owner.OrganizationId, runId, token)).Should().Be(scoresBefore, "a no-op failure keeps the run's rows");
    }

    [Theory]
    [InlineData(404, InsightsErrorCodes.ScoringModelUnavailable)]
    [InlineData(409, InsightsErrorCodes.ScoringModelMismatch)]
    [InlineData(422, InsightsErrorCodes.ScoringServiceRejected)]
    public async Task RefusedCall_FailsTheRunWithItsStableCode(int status, string expectedCode)
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        await CreateOrderedContactsAsync(admin, owner.OrganizationId, 1, null, token);
        _scoring.RefuseWith = (HttpStatusCode)status;

        var runId = await StartRunAsync(admin, UpliftKind, token);
        await RunJobAsync(owner.OrganizationId, runId, token);

        var run = await ReadRunAsync(admin, runId, token);
        run.GetProperty(StatusProperty).GetString().Should().Be(RunFailed);
        run.GetProperty(ErrorCodeProperty).GetString().Should().Be(expectedCode);
    }

    [Fact]
    public async Task StartWithTheSwitchOff_IsRefusedWithAConflict()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SyncModelsAsync(token);

        var response = await admin.PostAsJsonAsync(InsightsRoutes.ScoringRuns, new { kind = UpliftKind }, token);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await AuthTestClient.ReadJsonAsync(response, token)).GetProperty(CodeProperty).GetString()
            .Should().Be(InsightsErrorCodes.ScoringMlDisabled);
    }

    [Fact]
    public async Task SecondStartOfTheSameKind_IsRefusedWhileTheFirstIsQueued()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        await StartRunAsync(admin, UpliftKind, token);

        var second = await admin.PostAsJsonAsync(InsightsRoutes.ScoringRuns, new { kind = UpliftKind }, token);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await AuthTestClient.ReadJsonAsync(second, token)).GetProperty(CodeProperty).GetString()
            .Should().Be(InsightsErrorCodes.ScoringRunInProgress);
    }

    [Fact]
    public async Task Viewer_CannotStartAScoringRun()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        var viewer = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.ViewerRoleId, token);

        var response = await CrmApi.Client(_factory, viewer.AccessToken)
            .PostAsJsonAsync(InsightsRoutes.ScoringRuns, new { kind = UpliftKind }, token);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Representative_SeesSegmentChips_AndNoScoreNumbers()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var representative = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var contactId = await CreateContactAsync(admin, "Owned", representative.UserId, token);
        await InsertOrderAsync(owner.OrganizationId, contactId, token);
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);
        var scoresRoute = ContactScoresRoute(contactId);

        var representativeView = await AuthTestClient.ReadJsonAsync(
            await CrmApi.Client(_factory, representative.AccessToken).GetAsync(scoresRoute, token),
            token);
        var adminView = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(scoresRoute, token), token);

        representativeView.GetProperty(UpliftSegmentProperty).GetString().Should().Be(Persuadable);
        representativeView.GetProperty(ScoresProperty).EnumerateArray()
            .Should().OnlyContain(entry =>
                entry.GetProperty(ValueProperty).ValueKind == JsonValueKind.Null
                && entry.GetProperty(PTreatedProperty).ValueKind == JsonValueKind.Null
                && entry.GetProperty(PControlProperty).ValueKind == JsonValueKind.Null
                && entry.GetProperty(CurrencyProperty).ValueKind == JsonValueKind.Null,
                "a rep sees the chips and the provenance, never the numbers");
        representativeView.GetProperty(ScoresProperty).EnumerateArray()
            .SelectMany(entry => entry.EnumerateObject())
            .Select(property => property.Name)
            .Should().NotContain(PayloadProperty, "the raw payload is not part of the contract");
        var adminItem = adminView.GetProperty(ScoresProperty).EnumerateArray().Single();
        adminItem.GetProperty(ValueProperty).ValueKind.Should().Be(JsonValueKind.Number);
        adminItem.GetProperty(PTreatedProperty).GetDecimal().Should().Be(0.3m);
        adminItem.GetProperty(PControlProperty).GetDecimal().Should().Be(0.1m);
        adminItem.GetProperty(CurrencyProperty).ValueKind.Should().Be(JsonValueKind.Null, "an uplift estimate is not money");
    }

    [Fact]
    public async Task MlPriority_ReportsTheMlMode_AndKeepsADoNotDisturbContactLastWithACaution()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var kept = await CreateContactAsync(admin, "Kept", null, token);
        var caution = await CreateContactAsync(admin, "Caution", null, token);
        await InsertOrderAsync(owner.OrganizationId, kept, token);
        await InsertOrderAsync(owner.OrganizationId, caution, token);
        _scoring.DoNotDisturbSubjects.Add(caution.ToString("D"));
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);

        await RunDailyJobAsync(owner.OrganizationId, token);
        await SetSnapshotSegmentAsync(owner.OrganizationId, kept, "at_risk", token);
        await SetSnapshotSegmentAsync(owner.OrganizationId, caution, "champions", token);
        await RunRebuildJobAsync(owner.OrganizationId, token);

        var page = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Priority, token), token);
        page.GetProperty(ModeProperty).GetString().Should().Be("ml");
        page.GetProperty(MlStatusProperty).GetString().Should().Be("ready");
        page.GetProperty(ModelProvenanceProperty).GetString().Should().Be("semi_synthetic", "the list names the model that ranked it");
        page.GetProperty(UpliftHighThresholdProperty).GetDecimal().Should().Be(ConfiguredUpliftHighThreshold, "the list carries the configured high-uplift mark");
        page.GetProperty(StaleDealDaysProperty).GetInt32().Should().Be(ConfiguredStaleDealDays, "the list carries the configured stale-deal window");
        var items = page.GetProperty(ItemsProperty).EnumerateArray().ToList();
        var keptItem = items.Single(item => item.GetProperty(ContactIdProperty).GetGuid() == kept);
        var cautionItem = items.Single(item => item.GetProperty(ContactIdProperty).GetGuid() == caution);
        var keptReasons = keptItem.GetProperty(ReasonCodesProperty).EnumerateArray().Select(code => code.GetString()).ToList();
        keptReasons.Should().Contain(ReasonUpliftHigh, "the uplift reaches the configured high mark")
            .And.NotContain(ReasonPersuadable, "the uplift segment is a column now, not a reason")
            .And.NotContain(ReasonAtRisk, "the RFM segment is a column, not a reason");
        keptItem.GetProperty(UpliftSegmentProperty).GetString().Should().Be(Persuadable);
        keptItem.GetProperty(UpliftValueProperty).GetDecimal().Should().Be(FakeScoringService.Uplift);
        cautionItem.GetProperty(UpliftSegmentProperty).GetString().Should().Be(DoNotDisturbSegment);
        cautionItem.GetProperty(UpliftValueProperty).GetDecimal().Should().Be(FakeScoringService.Uplift);
        cautionItem.GetProperty(ReasonCodesProperty).EnumerateArray().Select(code => code.GetString())
            .Should().Contain(ReasonDoNotDisturb, "the list keeps the contact and explains the caution");
        cautionItem.GetProperty(ValueProperty).GetDecimal().Should().Be(0m);
        cautionItem.GetProperty("rank").GetInt32().Should().BeGreaterThan(keptItem.GetProperty("rank").GetInt32());

        // Both filter dimensions narrow the same list; the two may be combined.
        var cautionOnly = await AuthTestClient.ReadJsonAsync(
            await admin.GetAsync($"{InsightsRoutes.Priority}?upliftSegment={DoNotDisturbSegment}", token),
            token);
        cautionOnly.GetProperty(ItemsProperty).EnumerateArray().Select(item => item.GetProperty(ContactIdProperty).GetGuid())
            .Should().Equal(caution);
        var combined = await AuthTestClient.ReadJsonAsync(
            await admin.GetAsync($"{InsightsRoutes.Priority}?segment=at_risk&upliftSegment={Persuadable}", token),
            token);
        combined.GetProperty(ItemsProperty).EnumerateArray().Select(item => item.GetProperty(ContactIdProperty).GetGuid())
            .Should().Equal(kept);
        var mismatch = await AuthTestClient.ReadJsonAsync(
            await admin.GetAsync($"{InsightsRoutes.Priority}?segment=at_risk&upliftSegment={DoNotDisturbSegment}", token),
            token);
        mismatch.GetProperty(ItemsProperty).GetArrayLength().Should().Be(0);
        var unknown = await admin.GetAsync($"{InsightsRoutes.Priority}?upliftSegment=loyal", token);
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(unknown, "upliftSegment", token)).Should().Be(ErrorCodes.ValidationFailed);

        // The notification channel source leaves the caution out, so a chat is never told to call a do-not-disturb contact.
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
        var channelEntries = await scope.ServiceProvider.GetRequiredService<IPriorityReader>().ReadTopAsync(null, 10, token);
        channelEntries.Select(entry => entry.ContactId).Should().Contain(kept).And.NotContain(caution);
    }

    [Fact]
    public async Task PriorityWithTheSwitchOff_FallsBackToRfm_AndSaysWhy()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await CreateOrderedContactsAsync(admin, owner.OrganizationId, 2, null, token);

        await RunDailyJobAsync(owner.OrganizationId, token);

        var page = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Priority, token), token);
        page.GetProperty(ModeProperty).GetString().Should().Be("rfm");
        page.GetProperty(MlStatusProperty).GetString().Should().Be("disabled");
        page.GetProperty(ModelProvenanceProperty).ValueKind.Should().Be(JsonValueKind.Null);
        page.GetProperty(ItemsProperty).EnumerateArray().Should().OnlyContain(item =>
            item.GetProperty(UpliftValueProperty).ValueKind == JsonValueKind.Null
            && item.GetProperty(UpliftSegmentProperty).ValueKind == JsonValueKind.Null,
            "the RFM mode has no uplift numbers or segments to show");
    }

    [Fact]
    public async Task FreshOrganization_RebuildAfterAnUpliftRun_BuildsTheMlListWithoutADailyRun()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var kept = await CreateContactAsync(admin, "Kept", null, token);
        await InsertOrderAsync(owner.OrganizationId, kept, token);

        // No daily sweep has ever run for this organization: the scoring completion's rebuild must build the list itself.
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);
        await RunRebuildJobAsync(owner.OrganizationId, token);

        var page = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Priority, token), token);
        page.GetProperty(ModeProperty).GetString().Should().Be("ml");
        page.GetProperty(MlStatusProperty).GetString().Should().Be("ready");
        page.GetProperty(ItemsProperty).EnumerateArray()
            .Single(item => item.GetProperty(ContactIdProperty).GetGuid() == kept)
            .GetProperty(UpliftSegmentProperty).GetString().Should().Be(Persuadable);
    }

    [Fact]
    public async Task MlPriority_RanksAScoredContactAboveAnUnscoredContactOfAHigherWeightSegment()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var scored = await CreateContactAsync(admin, "Scored", null, token);
        var unscored = await CreateContactAsync(admin, "Unscored", null, token);
        await InsertOrderAsync(owner.OrganizationId, scored, token);
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);

        // The unscored contact gets its activity only after the run, so no uplift row exists for it. The segments are pinned
        // after the daily snapshot: scored is a champions contact (weight 60), unscored an at-risk one (weight 90).
        await InsertOrderAsync(owner.OrganizationId, unscored, token);
        await RunDailyJobAsync(owner.OrganizationId, token);
        await SetSnapshotSegmentAsync(owner.OrganizationId, scored, "champions", token);
        await SetSnapshotSegmentAsync(owner.OrganizationId, unscored, "at_risk", token);
        await RunRebuildJobAsync(owner.OrganizationId, token);

        var page = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Priority, token), token);
        var items = page.GetProperty(ItemsProperty).EnumerateArray().ToList();
        var scoredItem = items.Single(item => item.GetProperty(ContactIdProperty).GetGuid() == scored);
        var unscoredItem = items.Single(item => item.GetProperty(ContactIdProperty).GetGuid() == unscored);
        scoredItem.GetProperty(ModeProperty).GetString().Should().Be("ml");
        unscoredItem.GetProperty(ModeProperty).GetString().Should().Be("rfm", "the contact without a score keeps the RFM label");
        scoredItem.GetProperty("rank").GetInt32().Should().BeLessThan(unscoredItem.GetProperty("rank").GetInt32(),
            "30.6 (scored, 60/90 weight) ranks above 27 (unscored, 90/90 weight)");
    }

    [Fact]
    public async Task MlPriority_RepSeesNoValue_AndPagingStillWorks()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var representative = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var first = await CreateContactAsync(admin, "First", representative.UserId, token);
        var second = await CreateContactAsync(admin, "Second", representative.UserId, token);
        await InsertOrderAsync(owner.OrganizationId, first, token);
        await InsertOrderAsync(owner.OrganizationId, second, token);
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);
        await RunDailyJobAsync(owner.OrganizationId, token);

        using var rep = CrmApi.Client(_factory, representative.AccessToken);
        var firstPage = await AuthTestClient.ReadJsonAsync(await rep.GetAsync($"{InsightsRoutes.Priority}?limit=1", token), token);
        firstPage.GetProperty(ModeProperty).GetString().Should().Be("ml");
        var firstItem = firstPage.GetProperty(ItemsProperty).EnumerateArray().Single();
        firstItem.GetProperty(ValueProperty).ValueKind.Should().Be(JsonValueKind.Null, "a rep never gets the ML-derived value");
        var cursor = firstPage.GetProperty("nextCursor").GetString();
        cursor.Should().NotBeNullOrEmpty("a second contact follows on the next page");

        var secondPage = await AuthTestClient.ReadJsonAsync(
            await rep.GetAsync($"{InsightsRoutes.Priority}?limit=1&after={Uri.EscapeDataString(cursor!)}", token),
            token);
        var secondItem = secondPage.GetProperty(ItemsProperty).EnumerateArray().Single();
        secondItem.GetProperty(ContactIdProperty).GetGuid().Should().NotBe(firstItem.GetProperty(ContactIdProperty).GetGuid());
        secondItem.GetProperty(ValueProperty).ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ActiveModelWithoutARun_ReadsAsNoScores_AndATargetedCampaignIsRefused()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var contact = await CreateContactAsync(admin, "Scored", null, token);
        await InsertOrderAsync(owner.OrganizationId, contact, token);
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);

        // A newer uplift version becomes the one in use. The completed run scored with the old version, so it is not current.
        const string newerVersion = "crm_uplift-test-v2";
        await InsertUpliftVersionAsync(newerVersion, token);
        var version = await ReadMlSettingsVersionAsync(admin, token);
        (await admin.PutAsJsonAsync(
            InsightsRoutes.MlSettings,
            new { mlEnabled = true, version, activeUpliftVersion = newerVersion },
            token)).StatusCode.Should().Be(HttpStatusCode.OK);

        await RunDailyJobAsync(owner.OrganizationId, token);
        var page = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Priority, token), token);
        page.GetProperty(ModeProperty).GetString().Should().Be("rfm");
        page.GetProperty(MlStatusProperty).GetString().Should().Be("no_scores");

        var campaignId = await CreateTopUpliftCampaignAsync(admin, token);
        var prepare = await admin.PostAsync($"campaigns/{campaignId:D}/prepare", null, token);
        prepare.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await AuthTestClient.ReadJsonAsync(prepare, token)).GetProperty(CodeProperty).GetString()
            .Should().Be(CampaignsErrorCodes.CampaignScoresUnavailable);
    }

    [Fact]
    public async Task MlSwitchedOff_NoScoreIsCurrent_AndTheContactShowsNoMlItem()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var contact = await CreateContactAsync(admin, "Scored", null, token);
        await InsertOrderAsync(owner.OrganizationId, contact, token);
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);

        var version = await ReadMlSettingsVersionAsync(admin, token);
        (await admin.PutAsJsonAsync(InsightsRoutes.MlSettings, new { mlEnabled = false, version }, token))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var scores = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(ContactScoresRoute(contact), token), token);
        scores.GetProperty(ScoresProperty).EnumerateArray().Should().BeEmpty("the scores are not current while ML is off");
        scores.GetProperty(UpliftSegmentProperty).ValueKind.Should().Be(JsonValueKind.Null);

        var campaignId = await CreateTopUpliftCampaignAsync(admin, token);
        var prepare = await admin.PostAsync($"campaigns/{campaignId:D}/prepare", null, token);
        prepare.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await AuthTestClient.ReadJsonAsync(prepare, token)).GetProperty(CodeProperty).GetString()
            .Should().Be(CampaignsErrorCodes.CampaignScoresUnavailable);
    }

    [Fact]
    public async Task StuckRun_IsFailedBySweep_AndItsKindStartsAgain()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var stuck = await StartRunAsync(admin, UpliftKind, token);

        // The job of the run is lost: the run stays queued, and its start is old enough to be abandoned.
        await BackdateRunStartAsync(stuck, _factory.Clock.GetUtcNow().UtcDateTime.AddDays(-1), token);
        await RunSweepAsync(token);

        var run = await ReadRunAsync(admin, stuck, token);
        run.GetProperty(StatusProperty).GetString().Should().Be(ScoringRunStatuses.Failed);
        run.GetProperty(ErrorCodeProperty).GetString().Should().Be(InsightsErrorCodes.ScoringRunAbandoned);
        (await admin.PostAsJsonAsync(InsightsRoutes.ScoringRuns, new { kind = UpliftKind }, token))
            .StatusCode.Should().Be(HttpStatusCode.Accepted, "the abandoned run no longer blocks its kind");
    }

    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ScoringRunSweepJob>().ExecuteAsync(null, cancellationToken);
    }

    private async Task<Guid> CreateTopUpliftCampaignAsync(HttpClient admin, CancellationToken cancellationToken)
    {
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, cancellationToken);
        return await CampaignSendHarness.CreateCampaignAsync(
            admin,
            templateId,
            TestHoldoutShareBps,
            CampaignTargetingModes.TopUplift,
            TestTargetingShareBps,
            cancellationToken);
    }

    private async Task<uint> ReadMlSettingsVersionAsync(HttpClient admin, CancellationToken cancellationToken)
    {
        var settings = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.MlSettings, cancellationToken), cancellationToken);
        return settings.GetProperty("version").GetUInt32();
    }

    /// <summary>Adds an uplift version to the registry, as a sync would, so the settings can select it.</summary>
    private async Task InsertUpliftVersionAsync(string version, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO insights.model_versions
                (id, model_name, version, kind, feature_schema_version, provenance, trained_at, metrics, synced_at)
            VALUES (@id, @name, @version, 'uplift', 1, 'test', now(), '{}'::jsonb, now())
            """,
            connection);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("name", FakeScoringService.ModelName);
        command.Parameters.AddWithValue("version", version);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DateOnly TodayOf(IdentityApiFactory factory) => DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);

    /// <summary>Pins the segment of one snapshot row, so a test does not depend on the quintile tiers of a tiny fixture.</summary>
    private async Task SetSnapshotSegmentAsync(Guid organizationId, Guid contactId, string segment, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "UPDATE insights.rfm_snapshots SET segment = @segment WHERE organization_id = @org AND contact_id = @contact AND as_of = @day",
            connection);
        command.Parameters.AddWithValue("segment", segment);
        command.Parameters.AddWithValue(ParameterOrganization, organizationId);
        command.Parameters.AddWithValue(ParameterContact, contactId);
        command.Parameters.AddWithValue("day", TodayOf(_factory));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Ends a running run as failed, the way a stale sweep that raced with a live worker would.</summary>
    private async Task MarkRunFailedAsync(Guid runId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "UPDATE insights.scoring_runs SET status = 'failed', error_code = @code, finished_at = now() WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("code", InsightsErrorCodes.ScoringRunAbandoned);
        command.Parameters.AddWithValue("id", runId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task BackdateRunStartAsync(Guid runId, DateTime startedAt, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("UPDATE insights.scoring_runs SET started_at = @started WHERE id = @id", connection);
        command.Parameters.AddWithValue("started", startedAt);
        command.Parameters.AddWithValue("id", runId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    [Fact]
    public async Task MlPriority_BuiltBeforeTheRun_IsRankedByMlOnceTheRunCompletes()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var kept = await CreateContactAsync(admin, "Kept", null, token);
        var caution = await CreateContactAsync(admin, "Caution", null, token);
        await InsertOrderAsync(owner.OrganizationId, kept, token);
        await InsertOrderAsync(owner.OrganizationId, caution, token);
        _scoring.DoNotDisturbSubjects.Add(caution.ToString("D"));

        // The daily run builds the list before any scoring run exists: RFM rows, and the status says why.
        await RunDailyJobAsync(owner.OrganizationId, token);
        var before = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Priority, token), token);
        before.GetProperty(ModeProperty).GetString().Should().Be("rfm");
        before.GetProperty(MlStatusProperty).GetString().Should().Be("no_scores");
        before.GetProperty(ModelProvenanceProperty).ValueKind.Should().Be(JsonValueKind.Null, "the RFM list used no model");

        // The run completes; the rebuild its completion queues ranks the same day by the new scores.
        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);
        await RunRebuildJobAsync(owner.OrganizationId, token);

        var after = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Priority, token), token);
        after.GetProperty(ModeProperty).GetString().Should().Be("ml");
        after.GetProperty(MlStatusProperty).GetString().Should().Be("ready");
        var items = after.GetProperty(ItemsProperty).EnumerateArray().ToList();
        items.Select(item => item.GetProperty(ContactIdProperty).GetGuid()).Should().Contain([kept, caution]);
        items.Single(item => item.GetProperty(ContactIdProperty).GetGuid() == kept)
            .GetProperty(UpliftSegmentProperty).GetString().Should().Be(Persuadable);
        items.Single(item => item.GetProperty(ContactIdProperty).GetGuid() == caution)
            .GetProperty(ReasonCodesProperty).EnumerateArray().Select(code => code.GetString())
            .Should().Contain(ReasonDoNotDisturb);
    }

    [Fact]
    public async Task MlSettings_ActiveUpliftVersion_IsValidatedAndTheRegistryAgreesWithIt()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SyncModelsAsync(token);

        // No stored choice: the settings and the registry both report the newest synced uplift model as in use.
        var initial = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.MlSettings, token), token);
        initial.GetProperty("activeUpliftVersion").ValueKind.Should().Be(JsonValueKind.Null);
        initial.GetProperty("effectiveUpliftVersion").GetString().Should().Be(FakeScoringService.ModelVersion);
        var version = initial.GetProperty("version").GetUInt32();
        await AssertRegistryInUseAsync(admin, FakeScoringService.ModelVersion, token);

        // A version the registry does not hold is refused with the stable code, and nothing is stored.
        var unknown = await admin.PutAsJsonAsync(
            InsightsRoutes.MlSettings,
            new { mlEnabled = true, version, activeUpliftVersion = "no-such-version" },
            token);
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await AuthTestClient.ReadJsonAsync(unknown, token)).GetProperty(CodeProperty).GetString()
            .Should().Be(InsightsErrorCodes.MlSettingsModelUnknown);

        // A synced version is stored, and it is the one in use everywhere.
        var chosen = await admin.PutAsJsonAsync(
            InsightsRoutes.MlSettings,
            new { mlEnabled = true, version, activeUpliftVersion = FakeScoringService.ModelVersion },
            token);
        chosen.StatusCode.Should().Be(HttpStatusCode.OK);
        var chosenBody = await AuthTestClient.ReadJsonAsync(chosen, token);
        chosenBody.GetProperty("activeUpliftVersion").GetString().Should().Be(FakeScoringService.ModelVersion);
        chosenBody.GetProperty("effectiveUpliftVersion").GetString().Should().Be(FakeScoringService.ModelVersion);
        await AssertRegistryInUseAsync(admin, FakeScoringService.ModelVersion, token);

        // An omitted field keeps the stored choice; an explicit null resets it to the newest synced model.
        var keptResponse = await admin.PutAsJsonAsync(
            InsightsRoutes.MlSettings,
            new { mlEnabled = false, version = chosenBody.GetProperty("version").GetUInt32() },
            token);
        var keptBody = await AuthTestClient.ReadJsonAsync(keptResponse, token);
        keptBody.GetProperty("activeUpliftVersion").GetString().Should().Be(FakeScoringService.ModelVersion);
        var reset = await admin.PutAsJsonAsync(
            InsightsRoutes.MlSettings,
            new { mlEnabled = false, version = keptBody.GetProperty("version").GetUInt32(), activeUpliftVersion = (string?)null },
            token);
        var resetBody = await AuthTestClient.ReadJsonAsync(reset, token);
        resetBody.GetProperty("activeUpliftVersion").ValueKind.Should().Be(JsonValueKind.Null);
        resetBody.GetProperty("effectiveUpliftVersion").GetString().Should().Be(FakeScoringService.ModelVersion);
    }

    [Fact]
    public async Task ModelsSync_RefreshesTheRegistry_AndAServiceFailureKeepsItsStableCode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);

        var synced = await admin.PostAsync(InsightsRoutes.ModelsSync, null, token);
        synced.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AuthTestClient.ReadJsonAsync(synced, token)).GetProperty("synced").GetInt32().Should().BeGreaterThanOrEqualTo(1);

        // A refusal the resilience handler does not retry, so the test stays fast. The client maps every status it does not
        // know to the unavailable code, the same code a service that cannot answer gets.
        _scoring.RefuseModelsWith = HttpStatusCode.BadRequest;
        try
        {
            var failed = await admin.PostAsync(InsightsRoutes.ModelsSync, null, token);
            failed.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            (await AuthTestClient.ReadJsonAsync(failed, token)).GetProperty(CodeProperty).GetString()
                .Should().Be(InsightsErrorCodes.ScoringServiceUnavailable);
        }
        finally
        {
            _scoring.RefuseModelsWith = null;
        }
    }

    [Fact]
    public async Task ModelsSync_MarksAVersionTheServiceNoLongerServes_AsUnavailable()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SyncModelsAsync(token);
        var initial = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Models, token), token);
        initial[0].GetProperty(IsAvailableProperty).GetBoolean().Should().BeTrue();

        // The service stops reporting the model. The row stays as history, but no run may pin it and no admin may activate it.
        _scoring.HideModels = true;
        try
        {
            await SyncModelsAsync(token);

            var models = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Models, token), token);
            var stale = models[0];
            stale.GetProperty(IsAvailableProperty).GetBoolean().Should().BeFalse();
            stale.GetProperty(IsActiveProperty).GetBoolean().Should().BeFalse();
            var settings = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.MlSettings, token), token);
            settings.GetProperty(EffectiveUpliftVersionProperty).ValueKind.Should().Be(JsonValueKind.Null);
        }
        finally
        {
            _scoring.HideModels = false;
        }
    }

    [Fact]
    public async Task ScoreReader_ResolvesTheLatestCompletedRun_AndAPinnedRunKeepsItsOwnRows()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var contacts = new List<Guid>();
        for (var index = 0; index < 2; index++)
        {
            var contactId = await CreateContactAsync(admin, $"Pinned{index}", null, token);
            await InsertOrderAsync(owner.OrganizationId, contactId, token);
            contacts.Add(contactId);
        }

        var first = await StartRunAsync(admin, UpliftKind, token);
        await RunJobAsync(owner.OrganizationId, first, token);
        var second = await StartRunAsync(admin, UpliftKind, token);
        await RunJobAsync(owner.OrganizationId, second, token);

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
        var reader = scope.ServiceProvider.GetRequiredService<IScoreReader>();
        (await reader.ReadLatestScoringRunAsync(ScoreKinds.Uplift, token)).Should().Be(second);

        var pinned = await reader.ReadLatestScoresAsync(ScoreKinds.Uplift, contacts, null, first, token);
        pinned.Should().HaveCount(2, "the first run keeps its own rows after the second one completes");
        pinned.Values.Should().OnlyContain(score => score.Segment == Persuadable && score.ModelVersion == FakeScoringService.ModelVersion);

        var latest = await reader.ReadLatestScoresAsync(ScoreKinds.Uplift, contacts, null, null, token);
        latest.Should().HaveCount(2);
    }

    [Fact]
    public async Task ModelsSync_StoresTheUpliftSegmentRules_AndToleratesTheirAbsence()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await SyncModelsAsync(token);

        var synced = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Models, token), token);
        var uplift = synced.EnumerateArray().Single(model => model.GetProperty("modelName").GetString() == FakeScoringService.ModelName);
        uplift.GetProperty(SegmentRulesProperty).GetProperty("persuadable_uplift_at_least").GetDecimal().Should().Be(0.1m);
        uplift.GetProperty(SegmentRulesProperty).GetProperty("sure_thing_control_rate_at_least").GetDecimal().Should().Be(0.35m);

        // An older service omits the field: the sync still succeeds and the rules read as unknown.
        _scoring.OmitSegmentRules = true;
        try
        {
            await SyncModelsAsync(token);
            var after = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Models, token), token);
            after.EnumerateArray().Single(model => model.GetProperty("modelName").GetString() == FakeScoringService.ModelName)
                .GetProperty(SegmentRulesProperty).ValueKind.Should().Be(JsonValueKind.Null);
        }
        finally
        {
            _scoring.OmitSegmentRules = false;
        }
    }

    [Fact]
    public async Task ContactScores_CarryTypedClvFields_AndTheOrganizationCurrency()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        await EnableMlAsync(admin, token);
        var representative = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var contactId = await CreateContactAsync(admin, "Clv", representative.UserId, token);
        await InsertLatestScoreAsync(owner.OrganizationId, contactId, "clv", 781.625m, ClvPayload, token);

        var adminView = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(ContactScoresRoute(contactId), token), token);
        var clv = adminView.GetProperty(ScoresProperty).EnumerateArray().Single();
        clv.GetProperty("kind").GetString().Should().Be("clv");
        clv.GetProperty(PAliveProperty).GetDecimal().Should().Be(0.91m);
        clv.GetProperty(ExpectedPurchasesProperty).GetDecimal().Should().Be(6.5m);
        clv.GetProperty(ExpectedAverageValueProperty).GetDecimal().Should().Be(120.25m);
        clv.GetProperty(HorizonDaysProperty).GetInt32().Should().Be(365);
        clv.GetProperty(CurrencyProperty).GetString().Should().Be(OrganizationDefaults.Currency);
        clv.TryGetProperty(PayloadProperty, out _).Should().BeFalse("the raw payload is replaced by typed fields");

        using var representativeClient = CrmApi.Client(_factory, representative.AccessToken);
        var representativeView = await AuthTestClient.ReadJsonAsync(
            await representativeClient.GetAsync(ContactScoresRoute(contactId), token),
            token);
        var hidden = representativeView.GetProperty(ScoresProperty).EnumerateArray().Single();
        hidden.GetProperty(PAliveProperty).ValueKind.Should().Be(JsonValueKind.Null);
        hidden.GetProperty(ExpectedPurchasesProperty).ValueKind.Should().Be(JsonValueKind.Null);
        hidden.GetProperty(ExpectedAverageValueProperty).ValueKind.Should().Be(JsonValueKind.Null);
        hidden.GetProperty(HorizonDaysProperty).ValueKind.Should().Be(JsonValueKind.Null);
        hidden.GetProperty(CurrencyProperty).ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task PrioritySegmentCounts_ListEverySegment_AndRespectTheScope()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        var representative = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        await EnableMlAsync(admin, token);
        await SyncModelsAsync(token);
        var representativeChampion = await CreateContactAsync(admin, "RepChampion", representative.UserId, token);
        var representativeLost = await CreateContactAsync(admin, "RepLost", representative.UserId, token);
        var organizationRisk = await CreateContactAsync(admin, "OrgRisk", null, token);
        foreach (var contactId in new[] { representativeChampion, representativeLost, organizationRisk })
        {
            await InsertOrderAsync(owner.OrganizationId, contactId, token);
        }

        await RunJobAsync(owner.OrganizationId, await StartRunAsync(admin, UpliftKind, token), token);
        await RunDailyJobAsync(owner.OrganizationId, token);
        await SetSnapshotSegmentAsync(owner.OrganizationId, representativeChampion, "champions", token);
        await SetSnapshotSegmentAsync(owner.OrganizationId, representativeLost, "lost", token);
        await SetSnapshotSegmentAsync(owner.OrganizationId, organizationRisk, "at_risk", token);
        await RunRebuildJobAsync(owner.OrganizationId, token);

        var today = TodayOf(_factory).ToString(InsightsLimits.DayFormat, CultureInfo.InvariantCulture);
        var organizationCounts = await AuthTestClient.ReadJsonAsync(
            await admin.GetAsync(InsightsRoutes.PrioritySegments, token),
            token);
        organizationCounts.GetProperty("asOf").GetString().Should().Be(today);
        organizationCounts.GetProperty("rfmAsOf").GetString().Should().Be(today);
        organizationCounts.GetProperty("upliftAsOf").GetString().Should().Be(today);
        organizationCounts.GetProperty("rfm").GetArrayLength().Should().Be(RfmSegmentNames.All.Count);
        organizationCounts.GetProperty("uplift").GetArrayLength().Should().Be(PriorityUpliftSegments.All.Count);
        SegmentCount(organizationCounts, "rfm", RfmSegmentNames.Champions).Should().Be((1, 1));
        SegmentCount(organizationCounts, "rfm", RfmSegmentNames.AtRisk).Should().Be((1, 1));
        SegmentCount(organizationCounts, "rfm", RfmSegmentNames.NoOrders).Should().Be((0, 0));
        SegmentCount(organizationCounts, "uplift", PriorityUpliftSegments.Persuadable).Should().Be((3, 3));
        SegmentCount(organizationCounts, "uplift", PriorityUpliftSegments.SureThing).Should().Be((0, 0));

        using var representativeClient = CrmApi.Client(_factory, representative.AccessToken);
        var representativeCounts = await AuthTestClient.ReadJsonAsync(
            await representativeClient.GetAsync(InsightsRoutes.PrioritySegments, token),
            token);
        SegmentCount(representativeCounts, "rfm", RfmSegmentNames.Champions).Should().Be((1, 1));
        SegmentCount(representativeCounts, "rfm", RfmSegmentNames.AtRisk).Should().Be((0, 0));
        SegmentCount(representativeCounts, "uplift", PriorityUpliftSegments.Persuadable).Should().Be((2, 2));
    }

    /// <summary>One row of a counts array: its on-list number and its total, or a failure naming the missing segment.</summary>
    private static (int OnList, int Total) SegmentCount(JsonElement counts, string arrayName, string segment)
    {
        var row = counts.GetProperty(arrayName).EnumerateArray()
            .Single(entry => entry.GetProperty(SegmentProperty).GetString() == segment);
        return (row.GetProperty("onList").GetInt32(), row.GetProperty("total").GetInt32());
    }

    private async Task InsertLatestScoreAsync(
        Guid organizationId,
        Guid contactId,
        string kind,
        decimal value,
        string payload,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO insights.latest_scores
                (organization_id, contact_id, score_kind, score_id, value, payload, model_name, model_version, scored_at)
            VALUES (@org, @contact, @kind, gen_random_uuid(), @value, CAST(@payload AS jsonb), @name, @version, now())
            """,
            connection);
        command.Parameters.AddWithValue(ParameterOrganization, organizationId);
        command.Parameters.AddWithValue(ParameterContact, contactId);
        command.Parameters.AddWithValue("kind", kind);
        command.Parameters.AddWithValue("value", value);
        command.Parameters.AddWithValue("payload", payload);
        command.Parameters.AddWithValue("name", "clv_bgnbd_gg");
        command.Parameters.AddWithValue("version", "clv-test-v1");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string ContactScoresRoute(Guid contactId) => $"contacts/{contactId:D}/scores";

    private async Task EnableMlAsync(HttpClient admin, CancellationToken cancellationToken)
    {
        var response = await admin.PutAsJsonAsync(InsightsRoutes.MlSettings, new { mlEnabled = true, version = 0 }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task SyncModelsAsync(CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ScoringModelsSyncJob>().ExecuteAsync(null, cancellationToken);
    }

    private static async Task<Guid> StartRunAsync(HttpClient admin, string kind, CancellationToken cancellationToken)
    {
        var response = await admin.PostAsJsonAsync(InsightsRoutes.ScoringRuns, new { kind }, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        return (await AuthTestClient.ReadJsonAsync(response, cancellationToken)).GetProperty(IdProperty).GetGuid();
    }

    private async Task RunJobAsync(Guid organizationId, Guid runId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RunScoringJob>().ExecuteAsync(organizationId, runId, cancellationToken);
    }

    private static async Task<JsonElement> ReadRunAsync(HttpClient admin, Guid runId, CancellationToken cancellationToken)
    {
        var response = await admin.GetAsync($"insights/scoring-runs/{runId:D}", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    private async Task CreateOrderedContactsAsync(HttpClient admin, Guid organizationId, int count, Guid? owner, CancellationToken cancellationToken)
    {
        for (var index = 0; index < count; index++)
        {
            var contactId = await CreateContactAsync(admin, $"Ordered{index}", owner, cancellationToken);
            await InsertOrderAsync(organizationId, contactId, cancellationToken);
        }
    }

    private static async Task<Guid> CreateContactAsync(HttpClient admin, string name, Guid? ownerUserId, CancellationToken cancellationToken)
    {
        var created = await CrmApi.CreateContactAsync(
            admin,
            new ContactBody(name, ContactLastName, null, null, null, DefaultCountry, null, null, ownerUserId, null),
            cancellationToken);
        return created.GetProperty(IdProperty).GetGuid();
    }

    private async Task InsertOrderAsync(Guid organizationId, Guid contactId, CancellationToken cancellationToken)
    {
        var orderedAt = _factory.Clock.GetUtcNow() - ThirtyDays;
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
        command.Parameters.AddWithValue(ParameterReference, $"scoring-{Guid.NewGuid():N}");
        command.Parameters.AddWithValue(ParameterOrderedAt, orderedAt);
        command.Parameters.AddWithValue(ParameterAmount, 100m);
        command.Parameters.AddWithValue(ParameterCurrency, CrmDemoContract.DemoCurrency);
        command.Parameters.AddWithValue(ParameterSource, OrderSources.Manual);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Runs the priority rebuild the scoring completion queues, in a scope, the way a Hangfire worker would.</summary>
    private async Task RunRebuildJobAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<InsightsPriorityRebuildJob>().ExecuteAsync(organizationId, cancellationToken);
    }

    /// <summary>The registry marks exactly one uplift model in use, and it is the given version.</summary>
    private static async Task AssertRegistryInUseAsync(HttpClient admin, string version, CancellationToken cancellationToken)
    {
        var models = await AuthTestClient.ReadJsonAsync(await admin.GetAsync(InsightsRoutes.Models, cancellationToken), cancellationToken);
        models.EnumerateArray()
            .Where(model => model.GetProperty("modelName").GetString() == FakeScoringService.ModelName
                && model.GetProperty(IsActiveProperty).GetBoolean())
            .Select(model => model.GetProperty("version").GetString())
            .Should().ContainSingle().Which.Should().Be(version);
    }

    private async Task RunDailyJobAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(_factory.Clock.GetUtcNow().UtcDateTime).ToString(InsightsLimits.DayFormat, CultureInfo.InvariantCulture);
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<InsightsOrganizationDailyJob>().ExecuteAsync(organizationId, day, cancellationToken);
    }

    private async Task<long> CountScoresOfRunAsync(Guid organizationId, Guid runId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ScoresOfRunSql, connection);
        command.Parameters.AddWithValue(ParameterOrganization, organizationId);
        command.Parameters.AddWithValue("run", runId);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private async Task<List<string>> ReadLatestIdsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(LatestIdsSql, connection);
        command.Parameters.AddWithValue(ParameterOrganization, organizationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ids = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetGuid(0).ToString("D"));
        }

        return ids;
    }

    private async Task<long> ScalarAsync(string sql, Guid organizationId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(ParameterOrganization, organizationId);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
