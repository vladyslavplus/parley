using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Parley.Api.Controllers.Campaigns;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Options;
using Parley.Campaigns.Sending.Mail;
using Parley.Crm.Domain.Activities;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Errors;
using Parley.Insights.Contracts.Scores;
using Parley.IntegrationTests.Access.Support;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Send;

/// <summary>
/// The send pipeline end to end, with a recording mail sender in place of SMTP. The tests run the send job themselves.
/// They cover who is emailed, that a recipient gets one email, what an interrupted or refused send becomes, the
/// idempotent start, and the permission gates.
/// </summary>
public sealed class CampaignSendTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    /// <summary>Share of the audience held out. Small, so a few contacts always reach treatment.</summary>
    private const int HoldoutShareBps = 1000;

    private const int HalfShareBps = 5000;

    private const int ContactCount = 8;

    /// <summary>Arm sizes pinned before the freeze, so no test depends on a chance split (see the harness).</summary>
    private const int MinimumHoldout = 1;

    private const int MinimumTreatment = 3;

    private const string IdempotencyKeyOne = "start-once";

    private const string IdempotencyKeyTwo = "start-after-draft";

    private const string IdempotencyKeyThree = "start-under-new-key";

    private const string IdProperty = "id";

    private const string StatusProperty = "status";

    private const string ProblemCodeProperty = "code";

    private const string ProgressTotal = "total";

    private const string ProgressQueued = "queued";

    private const string ProgressSent = "sent";

    private const string ProgressFailed = "failed";

    private const string ProgressUnknown = "unknown";

    private const string ProgressSuppressed = "suppressed";

    private const string CrmSchema = "crm";

    private const string CrmActivities = "activities";

    private const string ActivityTypeColumn = "type";

    private const string CampaignsSchema = "campaigns";

    private const string ParameterOrganization = "org";

    private const string ParameterCampaign = "campaign";

    private const string ParameterContact = "contact";

    private const string TestMessagePrefix = "test.";

    private const string OneClickUnsubscribeUrlPrefix = TestHostSettings.PublicApiBaseUrlForLinks + "/unsubscribe?token=";

    private const string PageUnsubscribeUrlPrefix = TestHostSettings.PublicBaseUrl + "/en/unsubscribe?token=";

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
    public async Task Send_EmailsOnlyTargetedTreatment_RecordsEachOnce_AndCompletes()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"ranked{index}"), token);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.TopUplift, HalfShareBps, token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token, MinimumHoldout, MinimumTreatment);

        var recipients = await CampaignSendHarness.ReadRecipientsAsync(_connectionString, campaignId, token);
        var targeted = recipients.Where(row => row.Arm == HoldoutArms.Treatment && row.IsTargeted).Select(row => row.ContactId).ToList();
        var outside = recipients.Where(row => !(row.Arm == HoldoutArms.Treatment && row.IsTargeted)).Select(row => row.ContactId).ToList();
        targeted.Should().NotBeEmpty("the ranking selects treatment contacts");

        // A targeted contact unsubscribes after the freeze: it keeps its place in the arm, but is recorded as suppressed.
        var suppressedContact = targeted[0];
        await InsertSuppressionForAsync(owner.OrganizationId, suppressedContact, token);

        var testSend = await admin.PostAsync(IdentityApi.Route(CampaignsRoutes.CampaignTestSend, campaignId), null, token);
        testSend.StatusCode.Should().Be(HttpStatusCode.NoContent);
        _sender.Calls.Should().ContainSingle(message => message.ToAddress == owner.Email && message.MessageId.StartsWith(TestMessagePrefix, StringComparison.Ordinal));
        (await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token)).Should().BeEmpty("a test send writes no claim");

        var started = await CampaignSendHarness.StartAsync(admin, campaignId, IdempotencyKeyOne, token);
        started.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await AuthTestClient.ReadJsonAsync(started, token)).GetProperty(StatusProperty).GetString().Should().Be(CampaignStatuses.Sending);

        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);

        var rows = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        rows.Select(row => row.ContactId).Should().BeEquivalentTo(targeted, "only targeted treatment contacts have a claim");
        rows.Should().ContainSingle(row => row.ContactId == suppressedContact && row.Status == SendStatuses.SkippedSuppressed);
        rows.Where(row => row.ContactId != suppressedContact).Should().OnlyContain(row => row.Status == SendStatuses.Sent && row.Attempts == 1);
        foreach (var contact in outside)
        {
            rows.Should().NotContain(row => row.ContactId == contact, "holdout and non-targeted contacts never get a claim");
        }

        var campaignMessages = CampaignMessages(campaignId);
        campaignMessages.Should().HaveCount(targeted.Count - 1, "the suppressed contact is never emailed");
        campaignMessages.Should().OnlyContain(message => message.OneClickUnsubscribeUrl.StartsWith(OneClickUnsubscribeUrlPrefix, StringComparison.Ordinal));
        campaignMessages.Should().OnlyContain(message => message.HtmlBody.Contains(PageUnsubscribeUrlPrefix, StringComparison.Ordinal));
        campaignMessages.Should().OnlyContain(message => message.PlainTextBody.Contains(PageUnsubscribeUrlPrefix, StringComparison.Ordinal));
        campaignMessages.Should().OnlyContain(message => !message.Subject.Contains('\r') && !message.Subject.Contains('\n'));

        var progress = await CampaignSendHarness.ReadProgressAsync(admin, campaignId, token);
        progress.GetProperty(ProgressTotal).GetInt32().Should().Be(targeted.Count);
        progress.GetProperty(ProgressSent).GetInt32().Should().Be(targeted.Count - 1);
        progress.GetProperty(ProgressSuppressed).GetInt32().Should().Be(1);
        progress.GetProperty(ProgressQueued).GetInt32().Should().Be(0);
        progress.GetProperty(ProgressFailed).GetInt32().Should().Be(0);
        progress.GetProperty(ProgressUnknown).GetInt32().Should().Be(0);
        (await CampaignSendHarness.ReadCampaignStatusAsync(_connectionString, campaignId, token)).Should().Be(CampaignStatuses.Completed);

        // A repeat run finds nothing to claim and sends nothing.
        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);
        CampaignMessages(campaignId).Should().HaveCount(targeted.Count - 1);

        await CampaignSendHarness.DeliverCampaignEventsAsync(_factory, token);
        (await CampaignSendHarness.CountAsync(_connectionString, CrmSchema, CrmActivities, ActivityTypeColumn, ActivityTypes.CampaignEmailSent, token))
            .Should().Be(targeted.Count - 1, "each sent email writes one timeline entry");
    }

    [Fact]
    public async Task Start_ReplaysTheStoredResult_ForTheSameKey_AndRejectsStartOutsideReady()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt("replay"), token);
        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);

        var draft = await CampaignSendHarness.StartAsync(admin, campaignId, IdempotencyKeyTwo, token);
        draft.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await AuthTestClient.ReadJsonAsync(draft, token)).GetProperty(ProblemCodeProperty).GetString()
            .Should().Be(CampaignsErrorCodes.CampaignInvalidState);

        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token);

        var first = await CampaignSendHarness.StartAsync(admin, campaignId, IdempotencyKeyOne, token);
        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var firstBody = await AuthTestClient.ReadJsonAsync(first, token);

        // The same key returns the stored response. A handler run would answer 409, because the campaign is sending.
        var replayed = await CampaignSendHarness.StartAsync(admin, campaignId, IdempotencyKeyOne, token);
        replayed.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await AuthTestClient.ReadJsonAsync(replayed, token)).GetProperty(IdProperty).GetGuid()
            .Should().Be(firstBody.GetProperty(IdProperty).GetGuid());

        var newKey = await CampaignSendHarness.StartAsync(admin, campaignId, IdempotencyKeyThree, token);
        newKey.StatusCode.Should().Be(HttpStatusCode.Conflict, "a campaign that already sends cannot start again under a new key");
        (await AuthTestClient.ReadJsonAsync(newKey, token)).GetProperty(ProblemCodeProperty).GetString()
            .Should().Be(CampaignsErrorCodes.CampaignInvalidState);
    }

    [Fact]
    public async Task Send_KilledAfterSendBeforeMark_YieldsUnknown_AndNeverResends()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"killed{index}"), token);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token, MinimumHoldout, MinimumTreatment);
        var treatment = await TreatmentContactsAsync(campaignId, token);
        var crashed = treatment[0];
        var expired = treatment[1];
        var crashedAddress = await ReadRecipientAddressAsync(campaignId, crashed, token);

        // The claim of the second contact is left behind by a worker that died: its lease ended before any outcome.
        await InsertExpiredClaimAsync(campaignId, expired, token);

        // The worker dies right after the server took the message: the mock throws after recording the call.
        _sender.Behavior = (message, _) => message.ToAddress == crashedAddress
            ? throw new InvalidOperationException("worker stopped after the send and before the mark")
            : CampaignMailOutcome.Accepted;
        await StartAndRunAsync(admin, owner.OrganizationId, campaignId, token);

        var rows = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        rows.Should().ContainSingle(row => row.ContactId == crashed && row.Status == SendStatuses.Unknown);
        rows.Should().ContainSingle(row => row.ContactId == expired && row.Status == SendStatuses.Unknown);
        _sender.CallsFor(MessageIdFor(campaignId, crashed)).Should().Be(1, "the crashed send was attempted once");
        _sender.CallsFor(MessageIdFor(campaignId, expired)).Should().Be(0, "a claim whose lease ended is never sent");

        // Running again never resends an unknown message.
        _sender.Behavior = static (_, _) => CampaignMailOutcome.Accepted;
        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);
        _sender.CallsFor(MessageIdFor(campaignId, crashed)).Should().Be(1);
        _sender.CallsFor(MessageIdFor(campaignId, expired)).Should().Be(0);
    }

    [Fact]
    public async Task Send_NotAccepted_RetriesUpToTheLimit_ThenFails()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"retry{index}"), token);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token, MinimumHoldout, MinimumTreatment);
        var treatment = await TreatmentContactsAsync(campaignId, token);
        var refused = treatment[0];
        var flaky = treatment[1];
        var refusedAddress = await ReadRecipientAddressAsync(campaignId, refused, token);
        var flakyAddress = await ReadRecipientAddressAsync(campaignId, flaky, token);
        var maxAttempts = new CampaignsSendOptions().MaxAttempts;

        // The refused contact never connects; the flaky one connects on its last attempt.
        _sender.Behavior = (message, attempt) =>
        {
            if (message.ToAddress == refusedAddress)
            {
                return CampaignMailOutcome.NotAccepted;
            }

            return message.ToAddress == flakyAddress && attempt < maxAttempts
                ? CampaignMailOutcome.NotAccepted
                : CampaignMailOutcome.Accepted;
        };
        await StartAndRunAsync(admin, owner.OrganizationId, campaignId, token);

        var rows = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        rows.Should().ContainSingle(row => row.ContactId == refused && row.Status == SendStatuses.Failed && row.Attempts == maxAttempts);
        rows.Should().ContainSingle(row => row.ContactId == flaky && row.Status == SendStatuses.Sent && row.Attempts == maxAttempts);
        _sender.CallsFor(MessageIdFor(campaignId, refused)).Should().Be(maxAttempts, "the attempts stop at the limit");
        _sender.CallsFor(MessageIdFor(campaignId, flaky)).Should().Be(maxAttempts);
        (await CampaignSendHarness.ReadCampaignStatusAsync(_connectionString, campaignId, token)).Should().Be(CampaignStatuses.Completed);
    }

    [Fact]
    public async Task Send_TwoWorkersRunningTogether_EmailEachRecipientOnce()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"parallel{index}"), token);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token);
        var treatment = await TreatmentContactsAsync(campaignId, token);
        await StartAsync(admin, campaignId, token);

        await Task.WhenAll(
            CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token),
            CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token));

        var rows = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        rows.Should().HaveCount(treatment.Count);
        rows.Should().OnlyContain(row => row.Status == SendStatuses.Sent && row.Attempts == 1);
        foreach (var contact in treatment)
        {
            _sender.CallsFor(MessageIdFor(campaignId, contact)).Should().Be(1, "two workers never email one recipient twice");
        }
    }

    [Fact]
    public async Task Start_RequiresTheSendPermission_AndProgressRequiresTheReadPermission()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt("gate"), token);
        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token);

        // The viewer role reads campaigns but cannot send them.
        var viewerEmail = AuthTestClient.UniqueEmail();
        await AccessTestData.AddMemberAsync(_factory.Services, owner.OrganizationId, SystemRoleSeed.ViewerRoleId, viewerEmail, _factory.Clock, token);
        var viewerTokens = await IdentityApi.SignInAsync(_factory, viewerEmail, token);
        using var viewer = CrmApi.Client(_factory, viewerTokens.AccessToken);

        var start = await CampaignSendHarness.StartAsync(viewer, campaignId, IdempotencyKeyOne, token);
        start.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await AuthTestClient.ReadJsonAsync(start, token)).GetProperty(ProblemCodeProperty).GetString()
            .Should().Be(IdentityErrorCodes.PermissionDenied);
        (await CampaignSendHarness.ReadCampaignStatusAsync(_connectionString, campaignId, token)).Should().Be(CampaignStatuses.Ready);

        var progress = await viewer.GetAsync(IdentityApi.Route(CampaignsRoutes.CampaignProgress, campaignId), token);
        progress.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private IReadOnlyList<CampaignMailMessage> CampaignMessages(Guid campaignId) =>
        _sender.Calls.Where(message => message.MessageId.StartsWith(campaignId.ToString("D"), StringComparison.Ordinal)).ToArray();

    private async Task<List<Guid>> TreatmentContactsAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var recipients = await CampaignSendHarness.ReadRecipientsAsync(_connectionString, campaignId, cancellationToken);
        var treatment = recipients.Where(row => row.Arm == HoldoutArms.Treatment).Select(row => row.ContactId).ToList();
        treatment.Should().HaveCountGreaterThanOrEqualTo(2, "the arm assignment leaves at least two treatment contacts");
        return treatment;
    }

    private async Task StartAsync(HttpClient admin, Guid campaignId, CancellationToken cancellationToken)
    {
        var started = await CampaignSendHarness.StartAsync(admin, campaignId, IdempotencyKeyOne, cancellationToken);
        started.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    private async Task StartAndRunAsync(HttpClient admin, Guid organizationId, Guid campaignId, CancellationToken cancellationToken)
    {
        await StartAsync(admin, campaignId, cancellationToken);
        await CampaignSendHarness.RunSendJobAsync(_factory, organizationId, campaignId, cancellationToken);
    }

    /// <summary>Suppresses the address of a contact the way an unsubscribe does, with the manual reason.</summary>
    private async Task InsertSuppressionForAsync(Guid organizationId, Guid contactId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO campaigns.suppressions (id, organization_id, contact_id, email_normalized, reason, suppressed_at, created_at, updated_at)
            SELECT gen_random_uuid(), @org, @contact, lower(trim(r.email_snapshot)), 'manual', now(), now(), now()
            FROM campaigns.recipients r
            WHERE r.organization_id = @org AND r.contact_id = @contact
            """,
            connection);
        command.Parameters.AddWithValue(ParameterOrganization, organizationId);
        command.Parameters.AddWithValue(ParameterContact, contactId);
        (await command.ExecuteNonQueryAsync(cancellationToken)).Should().BeGreaterThan(0);
    }

    private async Task<string> ReadRecipientAddressAsync(Guid campaignId, Guid contactId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT email_snapshot FROM campaigns.recipients WHERE campaign_id = @campaign AND contact_id = @contact",
            connection);
        command.Parameters.AddWithValue(ParameterCampaign, campaignId);
        command.Parameters.AddWithValue(ParameterContact, contactId);
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    /// <summary>Writes a claim whose lease ended ten minutes ago and that never reached an outcome.</summary>
    private async Task InsertExpiredClaimAsync(Guid campaignId, Guid contactId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO campaigns.send_records (id, organization_id, created_at, updated_at, campaign_id, contact_id, recipient_id,
                status, attempt_count, claimed_at, lease_expires_at, message_id)
            SELECT gen_random_uuid(), r.organization_id, now(), now(), r.campaign_id, r.contact_id, r.id,
                'claimed', 1, now() - interval '10 minutes', now() - interval '1 minute', @message
            FROM campaigns.recipients r
            WHERE r.campaign_id = @campaign AND r.contact_id = @contact
            """,
            connection);
        command.Parameters.AddWithValue("message", MessageIdFor(campaignId, contactId));
        command.Parameters.AddWithValue(ParameterCampaign, campaignId);
        command.Parameters.AddWithValue(ParameterContact, contactId);
        (await command.ExecuteNonQueryAsync(cancellationToken)).Should().Be(1);
    }

    private static string MessageIdFor(Guid campaignId, Guid contactId) =>
        CampaignMessageIds.ForRecipient(campaignId, contactId, TestHostSettings.MessageIdDomain);
}
