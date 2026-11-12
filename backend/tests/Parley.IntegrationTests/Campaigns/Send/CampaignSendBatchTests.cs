using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Sending.Mail;
using Parley.Insights.Contracts.Scores;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Send;

/// <summary>
/// The send loop counts claims, not sendable messages: a batch whose recipients are all suppressed is progress, so the
/// runner must not stop and leave the rest of the audience for the recovery sweep.
/// </summary>
public sealed class CampaignSendBatchTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int HoldoutShareBps = 1000;

    private const int ContactCount = 6;

    private const int MinimumHoldout = 1;

    private const int MinimumTreatment = 3;

    private const string BatchSizeKey = "Campaigns:Send:BatchSize";

    private const string BatchSizeValue = "2";

    private const string FirstTreatmentRecipientsSql = """
        SELECT contact_id FROM campaigns.recipients
        WHERE organization_id = @org AND campaign_id = @campaign AND arm = 'treatment' AND is_targeted
        ORDER BY id
        LIMIT 2
        """;

    private const string SuppressionSql = """
        INSERT INTO campaigns.suppressions (id, organization_id, contact_id, email_normalized, reason, suppressed_at, created_at, updated_at)
        SELECT gen_random_uuid(), @org, r.contact_id, lower(trim(r.email_snapshot)), 'manual', now(), now(), now()
        FROM campaigns.recipients r
        WHERE r.organization_id = @org AND r.campaign_id = @campaign AND r.contact_id = @contact
        """;

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
            extraConfiguration: new Dictionary<string, string?> { [BatchSizeKey] = BatchSizeValue },
            extraServices: services =>
            {
                services.RemoveAll<ICampaignMailSender>();
                services.AddSingleton<ICampaignMailSender>(sender);
                services.RemoveAll<IScoreReader>();
                services.AddSingleton<IScoreReader, StubScoreReader>();
            });
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AWholeBatchOfSuppressedRecipients_DoesNotStopTheSend()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"batch{index}"), token);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token, MinimumHoldout, MinimumTreatment);

        // The two recipients the first batch claims are suppressed after the freeze: the batch yields no sendable message.
        var suppressed = await ReadFirstTreatmentRecipientsAsync(owner.OrganizationId, campaignId, token);
        suppressed.Should().HaveCount(2);
        foreach (var contactId in suppressed)
        {
            await SuppressAsync(owner.OrganizationId, campaignId, contactId, token);
        }

        (await CampaignSendHarness.StartAsync(admin, campaignId, "batch-start", token)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);

        var rows = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        rows.Where(row => suppressed.Contains(row.ContactId)).Should().OnlyContain(row => row.Status == SendStatuses.SkippedSuppressed);
        rows.Where(row => !suppressed.Contains(row.ContactId)).Should().NotBeEmpty("the rest of the treatment arm is sent in the same run");
        rows.Where(row => !suppressed.Contains(row.ContactId)).Should().OnlyContain(row => row.Status == SendStatuses.Sent);
        (await CampaignSendHarness.ReadCampaignStatusAsync(_connectionString, campaignId, token)).Should().Be(CampaignStatuses.Completed);
    }

    private async Task<List<Guid>> ReadFirstTreatmentRecipientsAsync(Guid organizationId, Guid campaignId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(FirstTreatmentRecipientsSql, connection);
        command.Parameters.AddWithValue("org", organizationId);
        command.Parameters.AddWithValue("campaign", campaignId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var contactIds = new List<Guid>();
        while (await reader.ReadAsync(cancellationToken))
        {
            contactIds.Add(reader.GetGuid(0));
        }

        return contactIds;
    }

    private async Task SuppressAsync(Guid organizationId, Guid campaignId, Guid contactId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SuppressionSql, connection);
        command.Parameters.AddWithValue("org", organizationId);
        command.Parameters.AddWithValue("campaign", campaignId);
        command.Parameters.AddWithValue("contact", contactId);
        (await command.ExecuteNonQueryAsync(cancellationToken)).Should().Be(1);
    }
}
