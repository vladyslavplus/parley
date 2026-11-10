using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Api;
using Parley.Api.Controllers.Campaigns;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Features.Campaigns.Prepare;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Persistence.Conventions;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Prepare;

/// <summary>
/// The prepare flow end to end: the endpoint queues the job, the job freezes the audience, suppressed contacts are in
/// neither arm, the commit point guards later edits, a resumed or repeated freeze rewrites the same audience, two runs
/// at once leave one audience, and the recovery sweep queues a stuck campaign again.
/// The job runs in the test scope, against a queue no server polls, so the outcome is deterministic.
/// </summary>
public sealed class CampaignPrepareTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string UnpolledQueueKey = "Campaigns:Prepare:QueueName";

    private const string UnpolledQueueName = "campaigns-prepare-unpolled";

    private const string RecoveryAgeKey = "Campaigns:Prepare:RecoveryAge";

    private const string ZeroRecoveryAge = "00:00:00";

    private const string SrmPValueProperty = "srmPValue";

    private const string JobsTable = "jobqueue";

    private const string IdProperty = "id";

    private const string VersionProperty = "version";

    private const string StatusProperty = "status";

    private const string CountProperty = "count";

    private const string TreatmentProperty = "treatment";

    private const string HoldoutProperty = "holdout";

    private const string AudienceFrozenAtProperty = "audienceFrozenAt";

    private const int HoldoutShareBps = 5000;

    private const string EmailDomain = "@prepare.example.test";

    private const string SuppressedReason = "manual";

    private const string Locale = "en";

    private const string TreatmentArm = "treatment";

    private const string HoldoutArm = "holdout";

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(
            postgres,
            mailpit,
            new Dictionary<string, string?>
            {
                [UnpolledQueueKey] = UnpolledQueueName,
                [RecoveryAgeKey] = ZeroRecoveryAge,
            });
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Prepare_FreezesTheAudience_LeavesSuppressedContactsOutOfBothArms_AndGuardsLaterEdits()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CreateTemplateAsync(admin, token);
        var eligible = new List<Guid>();
        for (var index = 0; index < 4; index++)
        {
            eligible.Add(CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactAt($"eligible{index}"), token)));
        }

        var suppressed = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactAt("suppressed"), token));
        await CrmApi.CreateContactAsync(admin, new ContactBody("No", "Email", null, null, null, "DE", null, null, null, null), token);
        await InsertSuppressionAsync(owner.OrganizationId, suppressed, "suppressed" + EmailDomain, token);

        var campaign = await CreateCampaignAsync(admin, templateId, token);
        var prepared = await admin.PostAsync(IdentityApi.Route(CampaignsRoutes.CampaignPrepare, campaign.Id), null, token);
        prepared.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await AuthTestClient.ReadJsonAsync(prepared, token)).GetProperty(StatusProperty).GetString().Should().Be("preparing");

        await RunPrepareJobAsync(owner.OrganizationId, campaign.Id, token);

        var review = await ReadReviewAsync(admin, campaign.Id, token);
        review.GetProperty(StatusProperty).GetString().Should().Be("ready");
        review.GetProperty(AudienceFrozenAtProperty).ValueKind.Should().NotBe(JsonValueKind.Null);
        var treatmentCount = review.GetProperty(TreatmentProperty).GetProperty(CountProperty).GetInt32();
        var holdoutCount = review.GetProperty(HoldoutProperty).GetProperty(CountProperty).GetInt32();
        (treatmentCount + holdoutCount).Should().Be(eligible.Count, "the suppressed and the address-less contacts are not recipients");

        (await CountRecipientsAsync(campaign.Id, suppressed, token)).Should().Be(0, "suppressed contacts enter neither arm");

        var edit = await admin.PutAsJsonAsync(
            IdentityApi.Route(CampaignsRoutes.CampaignById, campaign.Id),
            new { name = "Too late", templateId, holdoutShareBps = HoldoutShareBps, targetingMode = CampaignTargetingModesAll, version = campaign.Version },
            token);
        edit.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await AuthTestClient.ReadJsonAsync(edit, token);
        problem.GetProperty(ApiConstants.ProblemCode).GetString().Should().Be(CampaignsErrorCodes.CampaignAudienceFrozen);
    }

    [Fact]
    public async Task Prepare_ResumedAfterAnInterruption_ProducesTheSameAudience()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CreateTemplateAsync(admin, token);
        for (var index = 0; index < 3; index++)
        {
            await CrmApi.CreateContactAsync(admin, ContactAt($"resume{index}"), token);
        }

        var campaign = await CreateCampaignAsync(admin, templateId, token);
        await admin.PostAsync(IdentityApi.Route(CampaignsRoutes.CampaignPrepare, campaign.Id), null, token);
        await RunPrepareJobAsync(owner.OrganizationId, campaign.Id, token);
        var firstRun = await ReadRecipientRowsAsync(campaign.Id, token);
        firstRun.Should().NotBeEmpty();

        // An interruption before the commit point: the campaign is back in preparing. The next run rewrites the audience
        // from the pinned inputs, so the contents match, with new row ids.
        await ResetToPreparingAsync(campaign.Id, token);
        await RunPrepareJobAsync(owner.OrganizationId, campaign.Id, token);

        var resumed = await ReadRecipientRowsAsync(campaign.Id, token);
        resumed.Should().Equal(firstRun, "the same pinned inputs decide the same audience");
    }

    [Fact]
    public async Task Prepare_ResumedAfterAPartialWriteWithChangedCrmData_ProducesOneConsistentAudience()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CreateTemplateAsync(admin, token);
        var contacts = new List<Guid>();
        for (var index = 0; index < 3; index++)
        {
            contacts.Add(CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactAt($"partial{index}"), token)));
        }

        var campaign = await CreateCampaignAsync(admin, templateId, token);
        await admin.PostAsync(IdentityApi.Route(CampaignsRoutes.CampaignPrepare, campaign.Id), null, token);
        await RunPrepareJobAsync(owner.OrganizationId, campaign.Id, token);

        // An interruption after rows were written: the campaign is back in preparing and the rows are still there.
        await ResetToPreparingAsync(campaign.Id, token);
        var unfinished = await ReadReviewAsync(admin, campaign.Id, token);
        unfinished.GetProperty(TreatmentProperty).GetProperty(CountProperty).GetInt32().Should().Be(0, "an unfinished freeze is invisible");
        unfinished.GetProperty(HoldoutProperty).GetProperty(CountProperty).GetInt32().Should().Be(0, "an unfinished freeze is invisible");
        unfinished.GetProperty(SrmPValueProperty).ValueKind.Should().Be(JsonValueKind.Null, "an unfinished freeze has no SRM check");

        // Between the runs the CRM changes: one contact is suppressed by address, another by contact id after it changed
        // its address, and a new contact appears.
        await InsertSuppressionAsync(owner.OrganizationId, contacts[0], "partial0" + EmailDomain, token);
        await InsertSuppressionAsync(owner.OrganizationId, contacts[1], "moved-away" + EmailDomain, token);
        var late = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactAt("partial-late"), token));

        await RunPrepareJobAsync(owner.OrganizationId, campaign.Id, token);

        var review = await ReadReviewAsync(admin, campaign.Id, token);
        review.GetProperty(StatusProperty).GetString().Should().Be("ready");
        var treatmentCount = review.GetProperty(TreatmentProperty).GetProperty(CountProperty).GetInt32();
        var holdoutCount = review.GetProperty(HoldoutProperty).GetProperty(CountProperty).GetInt32();
        (treatmentCount + holdoutCount).Should().Be(2, "one original contact and the new one; the two suppressed ones are gone");
        (await CountRecipientsAsync(campaign.Id, contacts[0], token)).Should().Be(0, "a contact suppressed after the first run is not kept");
        (await CountRecipientsAsync(campaign.Id, contacts[1], token)).Should().Be(0, "a suppression by contact id holds when the address changed");
        (await CountRecipientsAsync(campaign.Id, late, token)).Should().Be(1, "a contact added after the first run is included");
    }

    [Fact]
    public async Task Prepare_TwoRunsAtOnce_LeaveOneConsistentAudience()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CreateTemplateAsync(admin, token);
        for (var index = 0; index < 4; index++)
        {
            await CrmApi.CreateContactAsync(admin, ContactAt($"twice{index}"), token);
        }

        var campaign = await CreateCampaignAsync(admin, templateId, token);
        await admin.PostAsync(IdentityApi.Route(CampaignsRoutes.CampaignPrepare, campaign.Id), null, token);

        // Two workers take the same campaign: one freezes it, the other exits at the lock or after the commit point.
        await Task.WhenAll(
            RunPrepareJobAsync(owner.OrganizationId, campaign.Id, token),
            RunPrepareJobAsync(owner.OrganizationId, campaign.Id, token));

        var review = await ReadReviewAsync(admin, campaign.Id, token);
        review.GetProperty(StatusProperty).GetString().Should().Be("ready");
        var treatmentCount = review.GetProperty(TreatmentProperty).GetProperty(CountProperty).GetInt32();
        var holdoutCount = review.GetProperty(HoldoutProperty).GetProperty(CountProperty).GetInt32();
        (treatmentCount + holdoutCount).Should().Be(4, "each eligible contact is one recipient, not two");
        (await ReadRecipientRowsAsync(campaign.Id, token)).Should().HaveCount(4);
    }

    [Fact]
    public async Task Prepare_StuckInPreparing_IsQueuedAgainBySweep_AndFreezesOnce()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CreateTemplateAsync(admin, token);
        for (var index = 0; index < 3; index++)
        {
            await CrmApi.CreateContactAsync(admin, ContactAt($"stuck{index}"), token);
        }

        var campaign = await CreateCampaignAsync(admin, templateId, token);
        var queuedBefore = await CountQueuedPrepareJobsAsync(token);
        await admin.PostAsync(IdentityApi.Route(CampaignsRoutes.CampaignPrepare, campaign.Id), null, token);
        (await CountQueuedPrepareJobsAsync(token)).Should().Be(queuedBefore + 1, "the prepare queues the freeze");

        // The sweep finds the campaign in preparing with no write for the recovery age, and queues it again.
        await RunPrepareRecoverySweepAsync(token);
        (await CountQueuedPrepareJobsAsync(token)).Should().Be(queuedBefore + 2, "the sweep queues the stuck campaign");

        await RunPrepareJobAsync(owner.OrganizationId, campaign.Id, token);
        var review = await ReadReviewAsync(admin, campaign.Id, token);
        review.GetProperty(StatusProperty).GetString().Should().Be("ready");
        (await ReadRecipientRowsAsync(campaign.Id, token)).Should().HaveCount(3);

        // A frozen campaign is not queued again.
        var queuedAfterFreeze = await CountQueuedPrepareJobsAsync(token);
        await RunPrepareRecoverySweepAsync(token);
        (await CountQueuedPrepareJobsAsync(token)).Should().Be(queuedAfterFreeze);
    }

    private static readonly string CampaignTargetingModesAll = Parley.Campaigns.Domain.Campaigns.CampaignTargetingModes.All;

    private static ContactBody ContactAt(string name) =>
        new("Name", name, name + EmailDomain, null, null, "DE", null, null, null, null);

    private static async Task<Guid> CreateTemplateAsync(HttpClient admin, CancellationToken token)
    {
        var response = await admin.PostAsJsonAsync(
            CampaignsRoutes.Templates,
            new { name = "Welcome", subjectLiquid = "Hello", bodyLiquid = "Hi {{ contact.first_name }}", locale = Locale },
            token);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await AuthTestClient.ReadJsonAsync(response, token)).GetProperty(IdProperty).GetGuid();
    }

    private static async Task<CampaignHandle> CreateCampaignAsync(HttpClient admin, Guid templateId, CancellationToken token)
    {
        var response = await admin.PostAsJsonAsync(
            CampaignsRoutes.Campaigns,
            new { name = "Spring", templateId, holdoutShareBps = HoldoutShareBps, targetingMode = CampaignTargetingModesAll },
            token);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var view = await AuthTestClient.ReadJsonAsync(response, token);
        return new CampaignHandle(view.GetProperty(IdProperty).GetGuid(), view.GetProperty(VersionProperty).GetUInt32());
    }

    private static async Task<System.Text.Json.JsonElement> ReadReviewAsync(HttpClient admin, Guid campaignId, CancellationToken token)
    {
        var response = await admin.GetAsync(IdentityApi.Route(CampaignsRoutes.CampaignReview, campaignId), token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, token);
    }

    private async Task RunPrepareJobAsync(Guid organizationId, Guid campaignId, CancellationToken token)
    {
        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<PrepareCampaignJob>();
        await job.ExecuteAsync(organizationId, campaignId, token);
    }

    private async Task InsertSuppressionAsync(Guid organizationId, Guid contactId, string emailNormalized, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO campaigns.suppressions (id, organization_id, contact_id, email_normalized, reason, suppressed_at, created_at, updated_at)
            VALUES (gen_random_uuid(), @org, @contact, @email, @reason, now(), now(), now())
            """,
            connection);
        command.Parameters.AddWithValue("org", organizationId);
        command.Parameters.AddWithValue("contact", contactId);
        command.Parameters.AddWithValue("email", emailNormalized);
        command.Parameters.AddWithValue("reason", SuppressedReason);
        (await command.ExecuteNonQueryAsync(token)).Should().Be(1);
    }

    private async Task<long> CountRecipientsAsync(Guid campaignId, Guid contactId, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM campaigns.recipients WHERE campaign_id = @campaign AND contact_id = @contact",
            connection);
        command.Parameters.AddWithValue("campaign", campaignId);
        command.Parameters.AddWithValue("contact", contactId);
        return (long)(await command.ExecuteScalarAsync(token) ?? 0L);
    }

    private async Task ResetToPreparingAsync(Guid campaignId, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(
            "UPDATE campaigns.campaigns SET status = 'preparing', audience_frozen_at = NULL WHERE id = @campaign",
            connection);
        command.Parameters.AddWithValue("campaign", campaignId);
        (await command.ExecuteNonQueryAsync(token)).Should().Be(1);
    }

    /// <summary>
    /// Recipient rows in contact order: every column the freeze decides. The row id is left out, because a rewrite of the
    /// audience gives its rows new ids.
    /// </summary>
    private async Task<List<string>> ReadRecipientRowsAsync(Guid campaignId, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(
            """
            SELECT contact_id, arm, assignment_bucket, is_targeted, email_snapshot, locale
            FROM campaigns.recipients WHERE campaign_id = @campaign ORDER BY contact_id
            """,
            connection);
        command.Parameters.AddWithValue("campaign", campaignId);
        await using var reader = await command.ExecuteReaderAsync(token);
        var rows = new List<string>();
        while (await reader.ReadAsync(token))
        {
            rows.Add(string.Join('|', reader.GetValue(0), reader.GetString(1), reader.GetInt32(2), reader.GetBoolean(3), reader.GetString(4), reader.GetString(5)));
        }

        return rows;
    }

    /// <summary>Queued prepare jobs on the test queue. No server polls it, so every entry stays until the count is read.</summary>
    private Task<long> CountQueuedPrepareJobsAsync(CancellationToken token) =>
        CampaignSendHarness.CountAsync(postgres.ConnectionString, PersistenceConstants.HangfireSchema, JobsTable, "queue", UnpolledQueueName, token);

    private async Task RunPrepareRecoverySweepAsync(CancellationToken token)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CampaignPrepareRecoveryJob>().ExecuteAsync(null, token);
    }

    private sealed record CampaignHandle(Guid Id, uint Version);
}
