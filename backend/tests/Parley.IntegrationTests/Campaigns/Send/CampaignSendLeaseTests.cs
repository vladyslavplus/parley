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
/// A claim whose lease ran out before its mail was submitted is never sent. The runner renews only a live lease, and the
/// expiry at the start of its next batch turns the claims left behind into unknown instead of resending them.
/// </summary>
public sealed class CampaignSendLeaseTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int HoldoutShareBps = 1000;

    private const int ContactCount = 4;

    /// <summary>Arm sizes pinned before the freeze, so the sends never depend on a chance split (see the harness).</summary>
    private const int MinimumHoldout = 1;

    private const int MinimumTreatment = 2;

    /// <summary>
    /// Ends the lease of every other claim of the campaign that is still waiting. The shared test clock is not moved: a jump
    /// fires the background timers of the host, which is not what a slow mail does.
    /// </summary>
    private const string ExpireOtherLeasesSql = """
        UPDATE campaigns.send_records
        SET lease_expires_at = now() - interval '1 minute'
        WHERE campaign_id = @campaign AND status = 'claimed' AND message_id <> @message
        """;

    private const string ParameterCampaign = "campaign";

    private const string ParameterMessage = "message";

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
    public async Task ExpiredLease_BeforeSubmit_IsNotSent_AndBecomesUnknown()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"lease{index}"), token);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token, MinimumHoldout, MinimumTreatment);
        (await CampaignSendHarness.StartAsync(admin, campaignId, "lease-start", token)).StatusCode.Should().Be(HttpStatusCode.Accepted);

        // The first mail is slow: while it is in flight, the leases of the other claims of the batch run out.
        var connectionString = _connectionString;
        _sender.Behavior = (message, _) =>
        {
            ExpireOtherLeases(connectionString, campaignId, message.MessageId);
            return CampaignMailOutcome.Accepted;
        };

        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);

        _sender.Calls.Should().HaveCount(1, "only the first mail was submitted while its claim was still under lease");
        var afterFirstRun = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        afterFirstRun.Count(row => row.Status == SendStatuses.Sent).Should().Be(1);

        // The runner expires the leases at the start of its next batch, so the claims that lost their lease are unknown,
        // not claimed, and none of them was submitted.
        afterFirstRun.Where(row => row.Status != SendStatuses.Sent).Should().OnlyContain(row => row.Status == SendStatuses.Unknown);

        // A later run finds nothing to send: an unknown claim is never sent again.
        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);

        _sender.Calls.Should().HaveCount(1, "a claim with an unknown outcome is never sent again");
        var afterSecondRun = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        afterSecondRun.Should().BeEquivalentTo(afterFirstRun);
    }

    private static void ExpireOtherLeases(string connectionString, Guid campaignId, string messageId)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = new NpgsqlCommand(ExpireOtherLeasesSql, connection);
        command.Parameters.AddWithValue(ParameterCampaign, campaignId);
        command.Parameters.AddWithValue(ParameterMessage, messageId);
        command.ExecuteNonQuery();
    }
}
