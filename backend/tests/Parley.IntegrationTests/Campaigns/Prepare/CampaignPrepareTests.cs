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
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Prepare;

/// <summary>
/// The prepare flow end to end: the endpoint queues the job, the job freezes the audience, suppressed contacts are in
/// neither arm, the commit point guards later edits, and a resumed freeze keeps the rows it wrote.
/// The job runs in the test scope, against a queue no server polls, so the outcome is deterministic.
/// </summary>
public sealed class CampaignPrepareTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string UnpolledQueueKey = "Campaigns:Prepare:QueueName";

    private const string UnpolledQueueName = "campaigns-prepare-unpolled";

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
            new Dictionary<string, string?> { [UnpolledQueueKey] = UnpolledQueueName });
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
    public async Task Prepare_ResumedAfterAnInterruption_ProducesIdenticalRows()
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

        // An interruption before the commit point: the campaign is back in preparing, the rows are already written.
        await ResetToPreparingAsync(campaign.Id, token);
        await RunPrepareJobAsync(owner.OrganizationId, campaign.Id, token);

        var resumed = await ReadRecipientRowsAsync(campaign.Id, token);
        resumed.Should().Equal(firstRun, "the resumed freeze keeps every row it had written, ids included");
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

    /// <summary>Recipient rows in a stable order, every column that the freeze decides, including the row id.</summary>
    private async Task<List<string>> ReadRecipientRowsAsync(Guid campaignId, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, contact_id, arm, assignment_bucket, is_targeted, email_snapshot, locale
            FROM campaigns.recipients WHERE campaign_id = @campaign ORDER BY contact_id
            """,
            connection);
        command.Parameters.AddWithValue("campaign", campaignId);
        await using var reader = await command.ExecuteReaderAsync(token);
        var rows = new List<string>();
        while (await reader.ReadAsync(token))
        {
            rows.Add(string.Join('|', reader.GetValue(0), reader.GetValue(1), reader.GetString(2), reader.GetInt32(3), reader.GetBoolean(4), reader.GetString(5), reader.GetString(6)));
        }

        return rows;
    }

    private sealed record CampaignHandle(Guid Id, uint Version);
}
