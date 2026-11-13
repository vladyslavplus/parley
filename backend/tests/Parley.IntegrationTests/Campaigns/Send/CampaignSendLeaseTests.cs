using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Features.Campaigns.Dispatch.Claims;
using Parley.Campaigns.Features.Campaigns.Dispatch.Outcomes;
using Parley.Campaigns.Options;
using Parley.Campaigns.Sending.Mail;
using Parley.Insights.Contracts.Scores;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Send;

/// <summary>
/// The lease of a claim decides what an expired claim becomes. A claim that was never submitted goes back to the pool and
/// is sent on a later pass, exactly once. A claim submitted to the server has an unknown outcome and is never sent again.
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

    private const string ExpireAllLeasesSql = """
        UPDATE campaigns.send_records
        SET lease_expires_at = now() - interval '1 minute'
        WHERE campaign_id = @campaign AND status = 'claimed'
        """;

    private const string LastAttemptExpiredSql = """
        UPDATE campaigns.send_records
        SET attempt_count = @attempts, submitted_at = NULL, lease_expires_at = now() - interval '1 minute'
        WHERE campaign_id = @campaign AND contact_id = @contact
        """;

    private const string ErrorCodeSql = """
        SELECT error_code FROM campaigns.send_records
        WHERE campaign_id = @campaign AND contact_id = @contact
        """;

    private const string ParameterCampaign = "campaign";

    private const string ParameterMessage = "message";

    private const string ParameterId = "id";

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
    public async Task ExpiredLease_BeforeSubmit_IsSentLaterExactlyOnce()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var campaignId = await PrepareStartedCampaignAsync(owner, admin, token);

        // The first mail is slow: while it is in flight, the leases of the other claims of the batch run out. They were
        // never submitted, so the next pass claims them again and sends each one exactly once.
        var expired = false;
        _sender.Behavior = (message, _) =>
        {
            if (!expired)
            {
                expired = true;
                ExpireOtherLeases(campaignId, message.MessageId);
            }

            return CampaignMailOutcome.Accepted;
        };

        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);

        var rows = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        rows.Should().NotBeEmpty("the treatment arm has claims");
        rows.Should().OnlyContain(row => row.Status == SendStatuses.Sent, "a claim that never reached the server is retried");
        foreach (var row in rows)
        {
            _sender.CallsFor(MessageIdFor(campaignId, row.ContactId)).Should().Be(1, "a resent claim is still sent once");
        }
    }

    [Fact]
    public async Task ExpiredLease_AfterSubmit_IsUnknown_AndNeverResent()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var campaignId = await PrepareStartedCampaignAsync(owner, admin, token);

        // One claim is handed to the server and stamped, then its lease runs out before the outcome is recorded.
        Guid submittedContact;
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
            var claimer = scope.ServiceProvider.GetRequiredService<CampaignSendClaimer>();
            var batch = await claimer.ClaimBatchAsync(campaignId, token);
            var claimed = batch.Sendable[0];
            submittedContact = claimed.ContactId;
            (await claimer.RenewLeaseAsync(claimed, token)).Should().BeTrue();
        }

        await ExecuteAsync(ExpireAllLeasesSql, (ParameterCampaign, campaignId), token);
        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);

        var rows = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        rows.Should().ContainSingle(row => row.ContactId == submittedContact)
            .Which.Status.Should().Be(SendStatuses.Unknown, "a submitted claim with an expired lease has an unknown outcome");
        rows.Where(row => row.ContactId != submittedContact).Should().OnlyContain(row => row.Status == SendStatuses.Sent);
        _sender.CallsFor(MessageIdFor(campaignId, submittedContact)).Should().Be(0, "an unknown claim is never sent again");
    }

    [Fact]
    public async Task AnUnsubmittedClaimOnItsLastAttempt_ExpiresToFailedWithACode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var campaignId = await PrepareStartedCampaignAsync(owner, admin, token);
        var claim = await ClaimOneAsync(owner.OrganizationId, campaignId, token);
        var maxAttempts = _factory.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<CampaignsSendOptions>>().Value.MaxAttempts;
        await ExecuteAsync(
            LastAttemptExpiredSql,
            (ParameterCampaign, campaignId),
            token,
            ("contact", claim.ContactId),
            ("attempts", maxAttempts));

        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
            await scope.ServiceProvider.GetRequiredService<CampaignSendClaimer>().ExpireLeasesAsync(campaignId, token);
        }

        var row = (await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token))
            .Single(send => send.ContactId == claim.ContactId);
        row.Status.Should().Be(SendStatuses.Failed, "a never-submitted claim cannot stay retryable for ever");
        (await ReadErrorCodeAsync(campaignId, claim.ContactId, token)).Should().Be(CampaignsErrorCodes.SendMailUnavailable);
    }

    [Fact]
    public async Task AStaleClaim_CannotRenewOrRecordAfterAnotherWorkerReclaimsIt()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var campaignId = await PrepareStartedCampaignAsync(owner, admin, token);

        var stale = await ClaimOneAsync(owner.OrganizationId, campaignId, token);
        await ExecuteAsync(ExpireAllLeasesSql, (ParameterCampaign, campaignId), token);

        // Another worker resolves the expired claim and re-claims the same recipient under a new generation.
        ClaimedSend fresh;
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
            var claimer = scope.ServiceProvider.GetRequiredService<CampaignSendClaimer>();
            await claimer.ExpireLeasesAsync(campaignId, token);
            var reclaimed = await claimer.ClaimBatchAsync(campaignId, token);
            fresh = reclaimed.Sendable.Single(send => send.ContactId == stale.ContactId);
            fresh.AttemptCount.Should().Be(stale.AttemptCount + 1, "a re-claim is a new generation");
            (await claimer.RenewLeaseAsync(fresh, token)).Should().BeTrue();
        }

        // The stale holder can neither renew the new generation nor write an outcome over it.
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
            var claimer = scope.ServiceProvider.GetRequiredService<CampaignSendClaimer>();
            var recorder = scope.ServiceProvider.GetRequiredService<CampaignSendRecorder>();

            (await claimer.RenewLeaseAsync(stale, token)).Should().BeFalse("the claim generation moved on");
            await recorder.MarkUnknownAsync(stale, CampaignsErrorCodes.SendResultUnknown, token);

            var row = (await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token))
                .Single(send => send.ContactId == stale.ContactId);
            row.Status.Should().Be(SendStatuses.Claimed, "the stale holder records nothing");
            row.Attempts.Should().Be(fresh.AttemptCount);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
            await scope.ServiceProvider.GetRequiredService<CampaignSendRecorder>().MarkSentAsync(campaignId, fresh, token);
        }

        (await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token))
            .Single(send => send.ContactId == stale.ContactId)
            .Status.Should().Be(SendStatuses.Sent, "the worker that owns the generation records the outcome");
    }

    private async Task<ClaimedSend> ClaimOneAsync(Guid organizationId, Guid campaignId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(organizationId);
        var claimer = scope.ServiceProvider.GetRequiredService<CampaignSendClaimer>();
        return (await claimer.ClaimBatchAsync(campaignId, cancellationToken)).Sendable[0];
    }

    private async Task<Guid> PrepareStartedCampaignAsync(OrganizationOwner owner, HttpClient admin, CancellationToken cancellationToken)
    {
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, cancellationToken);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"lease{index}"), cancellationToken);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(
            admin,
            templateId,
            HoldoutShareBps,
            CampaignTargetingModes.All,
            null,
            cancellationToken);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, cancellationToken, MinimumHoldout, MinimumTreatment);
        (await CampaignSendHarness.StartAsync(admin, campaignId, "lease-start", cancellationToken)).StatusCode.Should().Be(HttpStatusCode.Accepted);
        return campaignId;
    }

    /// <summary>
    /// The treatment arm can be smaller than every contact, so the test asserts on the claims the campaign actually wrote.
    /// </summary>
    private static string MessageIdFor(Guid campaignId, Guid contactId) =>
        CampaignMessageIds.ForRecipient(campaignId, contactId, TestHostSettings.MessageIdDomain);

    private void ExpireOtherLeases(Guid campaignId, string messageId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        using var command = new NpgsqlCommand(ExpireOtherLeasesSql, connection);
        command.Parameters.AddWithValue(ParameterCampaign, campaignId);
        command.Parameters.AddWithValue(ParameterMessage, messageId);
        command.ExecuteNonQuery();
    }

    private async Task ExecuteAsync(string sql, (string Name, object Value) first, CancellationToken cancellationToken, params (string Name, object Value)[] rest)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(first.Name, first.Value);
        foreach (var (name, value) in rest)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<string?> ReadErrorCodeAsync(Guid campaignId, Guid contactId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(ErrorCodeSql, connection);
        command.Parameters.AddWithValue(ParameterCampaign, campaignId);
        command.Parameters.AddWithValue("contact", contactId);
        return await command.ExecuteScalarAsync(cancellationToken) as string;
    }
}
