using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Api;
using Parley.Api.Controllers.Campaigns;
using Parley.Campaigns.Domain.Holdout;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Features.Campaigns.Dispatch.Job;
using Parley.Campaigns.Features.Campaigns.Prepare;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;

namespace Parley.IntegrationTests.Campaigns.Send.Support;

/// <summary>
/// Shared arrangement of the send tests: a template, contacts, a prepared campaign, the start request with an idempotency
/// key, and direct runs of the prepare and send jobs in a test scope. The Hangfire queues of the test host are not
/// polled for campaign jobs, so every run is driven by the test.
/// </summary>
public static class CampaignSendHarness
{
    public const string EmailDomain = "@send.example.test";

    public const string IdempotencyKeyHeader = ApiConstants.IdempotencyKeyHeader;

    private const string Locale = "en";

    private const string IdProperty = "id";

    private const string VersionProperty = "version";

    private const int MaximumSeedAttempts = 1000;

    private const string CampaignParameter = "campaign";

    private const string OrganizationParameter = "org";

    private const string SeedParameter = "seed";

    private const string HoldoutShareSql = "SELECT holdout_share_bps FROM campaigns.campaigns WHERE id = @campaign";

    private const string EligibleContactsSql =
        "SELECT id FROM crm.contacts WHERE organization_id = @org AND deleted_at IS NULL AND email IS NOT NULL";

    private const string PinSeedSql = "UPDATE campaigns.campaigns SET holdout_seed = @seed WHERE id = @campaign";

    private const string StatusProperty = "status";

    private const string Subject = "Hello {{ contact.first_name }}";

    private const string Body = "<p>Hi {{ contact.first_name }} {{ contact.last_name }}, <a href=\"{{ unsubscribe_url }}\">unsubscribe</a></p>";

    public static ContactBody ContactAt(string name) =>
        new("Name", name, name + EmailDomain, null, null, "DE", null, null, null, null);

    public static async Task<Guid> CreateTemplateAsync(HttpClient admin, CancellationToken cancellationToken)
    {
        var response = await admin.PostAsJsonAsync(
            CampaignsRoutes.Templates,
            new { name = "Delivery", subjectLiquid = Subject, bodyLiquid = Body, locale = Locale },
            cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await AuthTestClient.ReadJsonAsync(response, cancellationToken)).GetProperty(IdProperty).GetGuid();
    }

    /// <summary>Creates a campaign. A ranking mode needs a targeting share.</summary>
    public static async Task<Guid> CreateCampaignAsync(
        HttpClient admin,
        Guid templateId,
        int holdoutShareBps,
        string targetingMode,
        int? targetingShareBps,
        CancellationToken cancellationToken)
    {
        var response = await admin.PostAsJsonAsync(
            CampaignsRoutes.Campaigns,
            new
            {
                name = "Delivery campaign",
                templateId,
                holdoutShareBps,
                targetingMode,
                targetingShareBps,
            },
            cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await AuthTestClient.ReadJsonAsync(response, cancellationToken)).GetProperty(IdProperty).GetGuid();
    }

    /// <summary>
    /// Prepares a campaign and runs the freeze. A non-zero minimum pins the holdout seed first, so both arms reach the
    /// minimum: see <see cref="PinArmsAsync"/>. Zero leaves the seed of the campaign as created.
    /// </summary>
    public static async Task PrepareAsync(
        IdentityApiFactory factory,
        HttpClient admin,
        Guid organizationId,
        Guid campaignId,
        CancellationToken cancellationToken,
        int minimumHoldout = 0,
        int minimumTreatment = 0)
    {
        if (minimumHoldout > 0 || minimumTreatment > 0)
        {
            await PinArmsAsync(factory, organizationId, campaignId, minimumHoldout, minimumTreatment, cancellationToken);
        }

        var response = await admin.PostAsync(IdentityApi.Route(CampaignsRoutes.CampaignPrepare, campaignId), null, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await AuthTestClient.ReadJsonAsync(response, cancellationToken)).GetProperty(StatusProperty).GetString().Should().Be("preparing");

        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<PrepareCampaignJob>().ExecuteAsync(organizationId, campaignId, cancellationToken);
    }

    /// <summary>
    /// Pins the holdout seed of a campaign so both arms reach the minimum sizes. The arm of a contact depends on its random id,
    /// so an arm can be empty by chance. The candidate seeds are tried in a fixed order and the first split that meets the
    /// minimums is stored; the freeze reads the stored seed. Runs before the freeze, over the live contacts with an address.
    /// </summary>
    public static async Task PinArmsAsync(
        IdentityApiFactory factory,
        Guid organizationId,
        Guid campaignId,
        int minimumHoldout,
        int minimumTreatment,
        CancellationToken cancellationToken)
    {
        var dataSource = factory.Services.GetRequiredService<NpgsqlDataSource>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        int holdoutShareBps;
        await using (var share = new NpgsqlCommand(HoldoutShareSql, connection))
        {
            share.Parameters.AddWithValue(CampaignParameter, campaignId);
            holdoutShareBps = Convert.ToInt32(await share.ExecuteScalarAsync(cancellationToken));
        }

        var contacts = new List<Guid>();
        await using (var select = new NpgsqlCommand(EligibleContactsSql, connection))
        {
            select.Parameters.AddWithValue(OrganizationParameter, organizationId);
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                contacts.Add(reader.GetGuid(0));
            }
        }

        for (var attempt = 0; attempt < MaximumSeedAttempts; attempt++)
        {
            var seed = $"{campaignId:D}:{attempt}";
            var holdout = contacts.Count(contactId => HoldoutAssigner.Assign(seed, contactId, holdoutShareBps) == HoldoutArms.Holdout);
            if (holdout < minimumHoldout || contacts.Count - holdout < minimumTreatment)
            {
                continue;
            }

            await using var update = new NpgsqlCommand(PinSeedSql, connection);
            update.Parameters.AddWithValue(SeedParameter, seed);
            update.Parameters.AddWithValue(CampaignParameter, campaignId);
            await update.ExecuteNonQueryAsync(cancellationToken);
            return;
        }

        throw new InvalidOperationException("No holdout seed in the candidate range gives both arms the minimum size.");
    }

    /// <summary>Starts a ready campaign. The key makes a repeat return the stored response.</summary>
    public static async Task<HttpResponseMessage> StartAsync(HttpClient admin, Guid campaignId, string idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, IdentityApi.Route(CampaignsRoutes.CampaignStart, campaignId));
        request.Headers.Add(IdempotencyKeyHeader, idempotencyKey);
        return await admin.SendAsync(request, cancellationToken);
    }

    /// <summary>Runs the send job in a test scope, the way a Hangfire worker would.</summary>
    public static async Task RunSendJobAsync(IdentityApiFactory factory, Guid organizationId, Guid campaignId, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CampaignSendJob>().ExecuteAsync(organizationId, campaignId, cancellationToken);
    }

    /// <summary>Runs the recovery sweep in a test scope, the way the recurring Hangfire job would.</summary>
    public static async Task RunRecoverySweepAsync(IdentityApiFactory factory, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CampaignSendRecoveryJob>().ExecuteAsync(null, cancellationToken);
    }

    public static async Task<JsonElement> ReadProgressAsync(HttpClient admin, Guid campaignId, CancellationToken cancellationToken)
    {
        var response = await admin.GetAsync(IdentityApi.Route(CampaignsRoutes.CampaignProgress, campaignId), cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    /// <summary>Claim rows of a campaign: contact id, status and attempt count, in contact order.</summary>
    public static async Task<List<(Guid ContactId, string Status, int Attempts)>> ReadSendRowsAsync(string connectionString, Guid campaignId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT contact_id, status, attempt_count FROM campaigns.send_records WHERE campaign_id = @campaign ORDER BY contact_id",
            connection);
        command.Parameters.AddWithValue("campaign", campaignId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<(Guid, string, int)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2)));
        }

        return rows;
    }

    /// <summary>Recipient rows of a campaign: contact id, arm and the targeted flag.</summary>
    public static async Task<List<(Guid ContactId, string Arm, bool IsTargeted)>> ReadRecipientsAsync(string connectionString, Guid campaignId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT contact_id, arm, is_targeted FROM campaigns.recipients WHERE campaign_id = @campaign",
            connection);
        command.Parameters.AddWithValue("campaign", campaignId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<(Guid, string, bool)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2)));
        }

        return rows;
    }

    public static async Task<string> ReadCampaignStatusAsync(string connectionString, Guid campaignId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT status FROM campaigns.campaigns WHERE id = @campaign", connection);
        command.Parameters.AddWithValue("campaign", campaignId);
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    /// <summary>Count of rows of a table in the given schema that match a text column value.</summary>
    public static async Task<long> CountAsync(string connectionString, string schema, string table, string column, string value, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT count(*) FROM \"{schema}\".\"{table}\" WHERE \"{column}\" = @value",
            connection);
        command.Parameters.AddWithValue("value", value);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    /// <summary>Delivers the pending outbox rows of the campaigns module to their consumers, the way the dispatcher does.</summary>
    public static async Task DeliverCampaignEventsAsync(IdentityApiFactory factory, CancellationToken cancellationToken)
    {
        using var scope = factory.Services.CreateScope();
        await OutboxDelivery.DeliverDueAsync<Parley.Campaigns.Persistence.CampaignsDbContext>(scope.ServiceProvider, cancellationToken);
    }
}
