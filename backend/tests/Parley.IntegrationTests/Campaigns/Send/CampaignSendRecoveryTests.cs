using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Sending.Mail;
using Parley.Insights.Contracts.Scores;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Persistence.Conventions;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Send;

/// <summary>
/// The recovery sweep: a sending campaign whose runner is gone is queued again, and the queued run sends each recipient
/// once. A campaign that has completed is not queued again.
/// </summary>
public sealed class CampaignSendRecoveryTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int HoldoutShareBps = 1000;

    private const int ContactCount = 8;

    /// <summary>Arm sizes pinned before the freeze, so the sends never depend on a chance split (see the harness).</summary>
    private const int MinimumHoldout = 1;

    private const int MinimumTreatment = 3;

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
    public async Task RecoverySweep_QueuesASendingCampaignAgain_AndEachRecipientIsEmailedOnce()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"recovery{index}"), token);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token, MinimumHoldout, MinimumTreatment);

        var queuedBefore = await CountQueuedAsync(token);
        var started = await CampaignSendHarness.StartAsync(admin, campaignId, "recovery-start", token);
        started.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await CountQueuedAsync(token)).Should().Be(queuedBefore + 1, "the start queues the send job");

        // The test queue is never polled, so the start's job stands for a runner that never came. The sweep finds the
        // sending campaign with unclaimed work and no live claim, and queues it again.
        await CampaignSendHarness.RunRecoverySweepAsync(_factory, token);
        (await CountQueuedAsync(token)).Should().Be(queuedBefore + 2, "the sweep queues the sending campaign");

        // Both queued runs go through the claims. The first sends everything; the second finds nothing to claim.
        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);
        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);

        var rows = await CampaignSendHarness.ReadSendRowsAsync(_connectionString, campaignId, token);
        rows.Should().NotBeEmpty();
        rows.Should().OnlyContain(row => row.Status == SendStatuses.Sent && row.Attempts == 1);
        _sender.Calls.Should().HaveCount(rows.Count, "each recipient is emailed once");
        (await CampaignSendHarness.ReadCampaignStatusAsync(_connectionString, campaignId, token)).Should().Be(CampaignStatuses.Completed);

        // A completed campaign is not queued again.
        var queuedAfterCompletion = await CountQueuedAsync(token);
        await CampaignSendHarness.RunRecoverySweepAsync(_factory, token);
        (await CountQueuedAsync(token)).Should().Be(queuedAfterCompletion);
    }

    private Task<long> CountQueuedAsync(CancellationToken cancellationToken) =>
        CampaignSendHarness.CountAsync(
            _connectionString,
            PersistenceConstants.HangfireSchema,
            "jobqueue",
            "queue",
            TestHostSettings.UnpolledCampaignSendQueue,
            cancellationToken);
}
