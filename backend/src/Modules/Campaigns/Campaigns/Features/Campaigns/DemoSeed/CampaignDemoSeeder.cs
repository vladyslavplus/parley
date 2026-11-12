using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.DemoSeed;
using Parley.Campaigns.Contracts.Results;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.DemoSimulation;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Domain.Templates;
using Parley.Campaigns.Features.Campaigns.Attribution;
using Parley.Campaigns.Features.Campaigns.Dispatch.Job;
using Parley.Campaigns.Features.Campaigns.Prepare;
using Parley.Campaigns.Persistence;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Contracts.Metrics;
using Parley.Crm.Contracts.Outcomes;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.DemoSeed;

/// <summary>
/// Creates the historic demo campaigns of the demo seed (domain model 11). Both run through the production code: the
/// audience freeze, the send job and the attribution job. The send job routes <c>demo_seed</c> campaigns to the demo
/// sender, so nothing reaches SMTP. The injected-effect campaign also gets its simulated orders, written before its
/// attribution from the effect function of <c>ml/configs/crm_uplift_demo.yaml</c>, with every feature taken as of its send
/// time. Both campaigns are dated back to the dataset timeline: they start at the dataset end minus the send offset.
/// </summary>
public sealed class CampaignDemoSeeder(
    CampaignsDbContext dbContext,
    IMutableTenantContext tenantContext,
    PrepareCampaignJob prepareJob,
    CampaignSendJob sendJob,
    CampaignAttributionJob attributionJob,
    ICampaignResultsReader results,
    ICrmCustomerMetricsReader metrics,
    ICrmOutcomeReader outcomes,
    ICrmDemoSimulatedOrderWriter simulatedOrders) : ICampaignDemoSeeder
{
    public async Task<CampaignDemoSeedReport> SeedAsync(Guid organizationId, DateTimeOffset datasetEnd, CancellationToken cancellationToken)
    {
        tenantContext.Set(organizationId);
        var templateId = await EnsureTemplateAsync(organizationId, cancellationToken);

        var aaId = CampaignDemoSpecs.StableId(CampaignDemoSpecs.AaLabel);
        var aaStart = datasetEnd.AddDays(-CampaignDemoSpecs.AaSendOffsetDays);
        await EnsureCampaignAsync(organizationId, templateId, aaId, CampaignDemoSpecs.AaName, cancellationToken);
        await RunHistoricCampaignAsync(organizationId, aaId, aaStart, cancellationToken);
        await attributionJob.ExecuteAsync(organizationId, aaId, cancellationToken);
        var aaResults = await ReadResultsAsync(aaId, cancellationToken);

        var injectedId = CampaignDemoSpecs.StableId(CampaignDemoSpecs.InjectedLabel);
        var injectedStart = datasetEnd.AddDays(-CampaignDemoSpecs.InjectedSendOffsetDays);
        await EnsureCampaignAsync(organizationId, templateId, injectedId, CampaignDemoSpecs.InjectedName, cancellationToken);
        await RunHistoricCampaignAsync(organizationId, injectedId, injectedStart, cancellationToken);
        var effect = await WriteSimulatedEffectAsync(organizationId, injectedId, injectedStart, cancellationToken);
        await attributionJob.ExecuteAsync(organizationId, injectedId, cancellationToken);
        var injectedResults = await ReadResultsAsync(injectedId, cancellationToken);

        return new CampaignDemoSeedReport(
            aaResults,
            injectedResults,
            effect.TreatedContacts,
            effect.OrdersWritten,
            effect.NegativeRemovalsSkipped,
            effect.ConfiguredMeanEffect,
            effect.ExpectedWrittenLift);
    }

    private async Task<Guid> EnsureTemplateAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var templateId = CampaignDemoSpecs.StableId(CampaignDemoSpecs.TemplateLabel);
        if (await dbContext.Templates.AnyAsync(template => template.Id == templateId, cancellationToken))
        {
            return templateId;
        }

        dbContext.Templates.Add(new EmailTemplate
        {
            Id = templateId,
            OrganizationId = organizationId,
            Name = CampaignDemoSpecs.TemplateName,
            SubjectLiquid = CampaignDemoSpecs.TemplateSubject,
            BodyLiquid = CampaignDemoSpecs.TemplateBody,
            Locale = CampaignDemoSpecs.TemplateLocale,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return templateId;
    }

    private async Task EnsureCampaignAsync(
        Guid organizationId,
        Guid templateId,
        Guid campaignId,
        string name,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Campaigns.AnyAsync(campaign => campaign.Id == campaignId, cancellationToken))
        {
            return;
        }

        var campaign = new Campaign
        {
            Id = campaignId,
            OrganizationId = organizationId,
            Name = name,
            TemplateId = templateId,
            SegmentId = null,
            Status = CampaignStatuses.Draft,
            HoldoutShareBps = CampaignDemoSpecs.HoldoutShareBps,
            TargetingMode = CampaignTargetingModes.All,
            TargetingShareBps = null,
            AttributionWindowDays = CampaignDemoSpecs.WindowDays,
            ConversionRule = CampaignConversionRules.AnyOrder,
            Source = CampaignSources.DemoSeed,
        };
        campaign.HoldoutSeed = campaignId.ToString("D");
        dbContext.Campaigns.Add(campaign);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Moves a historic campaign through prepare and send, the way the API and the jobs do, with its dates set to the dataset
    /// timeline. A campaign that already completed is only re-dated, which changes nothing.
    /// </summary>
    private async Task RunHistoricCampaignAsync(Guid organizationId, Guid campaignId, DateTimeOffset startedAt, CancellationToken cancellationToken)
    {
        var status = await ReadStatusAsync(campaignId, cancellationToken);
        if (status == CampaignStatuses.Draft)
        {
            // The as-of instant is the send time, so the CRM counts of the audience are the counts of that day.
            await dbContext.Campaigns
                .Where(campaign => campaign.Id == campaignId && campaign.Status == CampaignStatuses.Draft)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(campaign => campaign.Status, CampaignStatuses.Preparing)
                    .SetProperty(campaign => campaign.PrepareAsOf, startedAt.UtcDateTime), cancellationToken);
            status = await ReadStatusAsync(campaignId, cancellationToken);
        }

        if (status is CampaignStatuses.Preparing or CampaignStatuses.Failed)
        {
            await prepareJob.ExecuteAsync(organizationId, campaignId, cancellationToken);
            status = await ReadStatusAsync(campaignId, cancellationToken);
        }

        if (status == CampaignStatuses.Ready)
        {
            await dbContext.Campaigns
                .Where(campaign => campaign.Id == campaignId && campaign.Status == CampaignStatuses.Ready)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(campaign => campaign.Status, CampaignStatuses.Sending)
                    .SetProperty(campaign => campaign.StartedAt, startedAt.UtcDateTime), cancellationToken);
            status = await ReadStatusAsync(campaignId, cancellationToken);
        }

        if (status == CampaignStatuses.Sending)
        {
            await sendJob.ExecuteAsync(organizationId, campaignId, cancellationToken);
            status = await ReadStatusAsync(campaignId, cancellationToken);
        }

        if (status != CampaignStatuses.Completed)
        {
            throw new InvalidOperationException("A historic demo campaign did not reach the completed state.");
        }

        // The completion instant moves onto the dataset timeline, a short fixed delay after the send instant, so the window
        // closes on the dataset timeline. The window of the outcomes still counts from the send instant, so the counts do not change.
        await dbContext.Campaigns
            .Where(campaign => campaign.Id == campaignId && campaign.Status == CampaignStatuses.Completed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(campaign => campaign.CompletedAt, startedAt.Add(CampaignDemoSpecs.CompletionDelay).UtcDateTime), cancellationToken);
    }

    /// <summary>
    /// Writes the simulated orders of the injected campaign. For each treatment-arm targeted contact: the baseline is whether a
    /// real order falls in the window, the effect comes from the segment rules as of the send time, and the treated outcome
    /// follows the documented rule. A negative effect would remove a real order, which the demo never does; it is counted.
    /// </summary>
    private async Task<SimulatedEffect> WriteSimulatedEffectAsync(
        Guid organizationId,
        Guid campaignId,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        var treated = await dbContext.Recipients
            .AsNoTracking()
            .Where(recipient => recipient.CampaignId == campaignId && recipient.Arm == HoldoutArms.Treatment && recipient.IsTargeted)
            .OrderBy(recipient => recipient.ContactId)
            .Select(recipient => recipient.ContactId)
            .ToListAsync(cancellationToken);
        var profiles = await ReadProfilesAsync(treated.ToHashSet(), startedAt, cancellationToken);
        var windowEnd = startedAt.AddDays(CampaignDemoSpecs.WindowDays);

        var ordersWritten = 0;
        var negativeSkipped = 0;
        var tauSum = 0d;
        var expectedWritten = 0d;
        for (var offset = 0; offset < treated.Count; offset += CrmCustomerMetricsContract.MaximumBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = treated.Skip(offset).Take(CrmCustomerMetricsContract.MaximumBatchSize).ToList();
            var rows = await outcomes.ReadWindowAsync(batch, startedAt, windowEnd, cancellationToken);
            var byContact = rows.ToDictionary(row => row.ContactId);

            var orders = new List<CrmSimulatedOrder>();
            foreach (var contactId in batch)
            {
                if (!profiles.TryGetValue(contactId, out var profile))
                {
                    throw new InvalidOperationException("A treated contact has no customer metrics as of the send time.");
                }

                var tau = DemoEffect.Tau(CampaignDemoFeatures.From(profile, startedAt));
                var baseline = byContact.TryGetValue(contactId, out var outcome) && outcome.FirstPurchaseAt is not null ? 1 : 0;
                var draw = DemoEffect.Draw(campaignId, contactId, CampaignDemoSpecs.OutcomeDrawPurpose);

                tauSum += tau;
                if (tau > 0d && baseline == 0)
                {
                    expectedWritten += tau;
                }

                if (tau < 0d && baseline == 1 && draw < -tau)
                {
                    negativeSkipped++;
                }

                // Only a non-buyer can gain an order; a positive effect is the only source of new orders.
                if (DemoEffect.TreatedOutcome(baseline, tau, draw) == 1 && baseline == 0)
                {
                    orders.Add(new CrmSimulatedOrder(
                        contactId,
                        ExternalRef(campaignId, contactId),
                        OrderedAt(startedAt, campaignId, contactId),
                        Amount(profile)));
                }
            }

            ordersWritten += await simulatedOrders.WriteAsync(organizationId, orders, cancellationToken);
        }

        var count = treated.Count;
        return new SimulatedEffect(
            count,
            ordersWritten,
            negativeSkipped,
            count == 0 ? 0d : tauSum / count,
            count == 0 ? 0d : expectedWritten / count);
    }

    /// <summary>Reads the customer metrics as of the send time for the wanted contacts, paging through every live contact.</summary>
    private async Task<Dictionary<Guid, CrmCustomerMetricsRow>> ReadProfilesAsync(
        HashSet<Guid> wanted,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        var profiles = new Dictionary<Guid, CrmCustomerMetricsRow>(wanted.Count);
        Guid? afterContactId = null;
        while (true)
        {
            var page = await metrics.ReadBatchAsync(afterContactId, CrmCustomerMetricsContract.MaximumBatchSize, asOf, cancellationToken);
            foreach (var row in page)
            {
                if (wanted.Contains(row.ContactId))
                {
                    profiles[row.ContactId] = row;
                }
            }

            if (page.Count < CrmCustomerMetricsContract.MaximumBatchSize)
            {
                return profiles;
            }

            afterContactId = page[^1].ContactId;
        }
    }

    /// <summary>A stable order instant strictly inside the window, drawn per contact.</summary>
    private static DateTimeOffset OrderedAt(DateTimeOffset startedAt, Guid campaignId, Guid contactId)
    {
        var windowSeconds = TimeSpan.FromDays(CampaignDemoSpecs.WindowDays).TotalSeconds;
        var draw = DemoEffect.Draw(campaignId, contactId, CampaignDemoSpecs.OrderTimeDrawPurpose);
        return startedAt.AddSeconds(1 + Math.Floor(draw * (windowSeconds - 2)));
    }

    private static decimal Amount(CrmCustomerMetricsRow profile) =>
        profile.AvgOrderValue > 0m ? decimal.Round(profile.AvgOrderValue, 2) : CampaignDemoSpecs.SimulatedOrderFallbackAmount;

    /// <summary>
    /// Stable reference of a simulated order, within the 64-character limit of the CRM reference column. The campaign part
    /// is a prefix of its id: the reference stays unique per contact and campaign, and a re-run finds the same row.
    /// </summary>
    private static string ExternalRef(Guid campaignId, Guid contactId) =>
        $"{CampaignDemoSpecs.ExternalRefPrefix}:{campaignId.ToString("N")[..CampaignDemoSpecs.CampaignRefLength]}:{contactId:N}";

    private async Task<string> ReadStatusAsync(Guid campaignId, CancellationToken cancellationToken) =>
        await dbContext.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == campaignId)
            .Select(campaign => campaign.Status)
            .SingleAsync(cancellationToken);

    private async Task<CampaignResultsSnapshot> ReadResultsAsync(Guid campaignId, CancellationToken cancellationToken) =>
        await results.FindAsync(campaignId, cancellationToken)
        ?? throw new InvalidOperationException("A historic demo campaign has no result.");

    private sealed record SimulatedEffect(
        int TreatedContacts,
        int OrdersWritten,
        int NegativeRemovalsSkipped,
        double ConfiguredMeanEffect,
        double ExpectedWrittenLift);
}
