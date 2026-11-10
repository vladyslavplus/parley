using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Holdout;
using Parley.Campaigns.Domain.Lift;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Shared;
using Parley.Crm.Contracts.Metrics;
using Parley.Crm.Contracts.Outcomes;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Attribution;

/// <summary>
/// Attribution of one campaign (domain model 7.7). For every targeted recipient of both arms it reads the orders and won
/// deals in <c>(started_at, started_at + window]</c>, with the same send time for both arms, and upserts one outcome row.
/// Intention to treat: the rows come from the frozen targeted recipients, so a failed or unknown send stays in its arm. Then
/// it recomputes the arm totals and the statistics and upserts the campaign result. Nothing here depends on the previous
/// run, so a re-run writes the same outcomes.
/// </summary>
public sealed class CampaignAttributionRunner(
    CampaignsDbContext dbContext,
    ITenantContext tenantContext,
    ICrmOutcomeReader outcomes,
    IOptions<CampaignsAnalysisOptions> analysis,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task RunAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.Campaigns
            .AsNoTracking()
            .Where(row => row.Id == campaignId)
            .Select(row => new
            {
                row.Status,
                row.StartedAt,
                row.CompletedAt,
                row.AttributionWindowDays,
                row.ConversionRule,
                row.HoldoutShareBps,
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Only a completed campaign has a send time and a completion instant. Anything else has nothing to attribute yet.
        if (campaign is null || campaign.Status != CampaignStatuses.Completed || campaign.StartedAt is null || campaign.CompletedAt is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var windowStart = new DateTimeOffset(DateTime.SpecifyKind(campaign.StartedAt.Value, DateTimeKind.Utc));
        var outcomeWindowEnd = windowStart.AddDays(campaign.AttributionWindowDays);

        var targeted = await dbContext.Recipients
            .AsNoTracking()
            .Where(recipient => recipient.CampaignId == campaignId && recipient.IsTargeted)
            .OrderBy(recipient => recipient.ContactId)
            .Select(recipient => new TargetedRecipient(recipient.Id, recipient.ContactId))
            .ToListAsync(cancellationToken);

        for (var offset = 0; offset < targeted.Count; offset += CrmCustomerMetricsContract.MaximumBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = targeted.Skip(offset).Take(CrmCustomerMetricsContract.MaximumBatchSize).ToList();
            var contactIds = batch.Select(recipient => recipient.ContactId).ToList();
            var rows = await outcomes.ReadWindowAsync(contactIds, windowStart, outcomeWindowEnd, cancellationToken);
            var byContact = rows.ToDictionary(row => row.ContactId);

            var outcomeRows = batch
                .Select(recipient => ToOutcomeRow(recipient, byContact.GetValueOrDefault(recipient.ContactId), campaign.ConversionRule, now))
                .ToList();
            await dbContext.Database.ExecuteSqlAsync(
                CampaignAttributionSql.UpsertOutcomes(
                    tenantContext.OrganizationId,
                    campaignId,
                    now,
                    JsonSerializer.Serialize(outcomeRows, JsonOptions)),
                cancellationToken);
        }

        var completedAt = new DateTimeOffset(DateTime.SpecifyKind(campaign.CompletedAt.Value, DateTimeKind.Utc));
        await RecomputeResultAsync(campaignId, campaign.HoldoutShareBps, completedAt, campaign.AttributionWindowDays, now, cancellationToken);
    }

    private async Task RecomputeResultAsync(
        Guid campaignId,
        int holdoutShareBps,
        DateTimeOffset completedAt,
        int windowDays,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var recipients = await dbContext.Recipients
            .AsNoTracking()
            .Where(recipient => recipient.CampaignId == campaignId)
            .Select(recipient => new RecipientRow(recipient.Id, recipient.Arm, recipient.IsTargeted))
            .ToListAsync(cancellationToken);
        var outcomeByRecipient = await dbContext.RecipientOutcomes
            .AsNoTracking()
            .Where(outcome => outcome.CampaignId == campaignId)
            .Select(outcome => new OutcomeRow(outcome.RecipientId, outcome.Converted, outcome.Revenue, outcome.SimulatedOrdersInWindow))
            .ToDictionaryAsync(outcome => outcome.RecipientId, cancellationToken);

        var treatmentAssigned = recipients.LongCount(recipient => recipient.Arm == HoldoutArms.Treatment);
        var holdoutAssigned = recipients.LongCount(recipient => recipient.Arm == HoldoutArms.Holdout);
        var treatment = ArmOf(recipients, HoldoutArms.Treatment, outcomeByRecipient);
        var holdout = ArmOf(recipients, HoldoutArms.Holdout, outcomeByRecipient);
        var simulated = recipients.Any(recipient => recipient.IsTargeted
            && outcomeByRecipient.TryGetValue(recipient.Id, out var outcome)
            && outcome.SimulatedOrdersInWindow > 0);

        var analysisOptions = analysis.Value;
        var result = LiftCalculator.Analyze(
            treatment.Arm,
            holdout.Arm,
            BasisPoints.Scale - holdoutShareBps,
            analysisOptions.ZAlpha,
            analysisOptions.ZBeta);

        // The sample ratio is checked on the assigned counts: assignment happens before targeting (domain model 7.3).
        var srm = treatmentAssigned + holdoutAssigned == 0
            ? (double?)null
            : SampleRatioCheck.PValue(holdoutAssigned, treatmentAssigned, holdoutShareBps);

        // Final only when the window has closed: completion plus the window length (domain model 7.5).
        var windowEndsAt = completedAt.AddDays(windowDays);
        var windowClosed = now >= windowEndsAt;

        await dbContext.Database.ExecuteSqlAsync(
            CampaignAttributionSql.UpsertResult(
                Guid.CreateVersion7(),
                tenantContext.OrganizationId,
                now,
                campaignId,
                treatmentAssigned,
                holdoutAssigned,
                treatment.Arm.Count,
                holdout.Arm.Count,
                treatment.Arm.Conversions,
                holdout.Arm.Conversions,
                treatment.Revenue,
                holdout.Revenue,
                result.RateTreatment,
                result.RateHoldout,
                result.Lift,
                result.CiLow,
                result.CiHigh,
                result.ZTestPValue,
                srm,
                result.MinimumDetectableEffect,
                result.IncrementalRevenue,
                simulated,
                windowEndsAt,
                windowClosed),
            cancellationToken);
    }

    private static TargetedOutcome ToOutcomeRow(TargetedRecipient recipient, CrmOutcomeRow? row, string conversionRule, DateTimeOffset now)
    {
        var purchaseAt = row?.FirstPurchaseAt;
        var wonAt = conversionRule == CampaignConversionRules.OrderOrWonDeal ? row?.FirstWonDealClosedAt : null;
        var firstConversion = purchaseAt is null
            ? wonAt
            : wonAt is null || purchaseAt <= wonAt
                ? purchaseAt
                : wonAt;

        return new TargetedOutcome(
            Guid.CreateVersion7(),
            recipient.RecipientId,
            recipient.ContactId,
            firstConversion is not null,
            firstConversion,
            row?.Revenue ?? 0m,
            row?.PurchaseCount ?? 0,
            row?.SimulatedPurchaseCount ?? 0);
    }

    /// <summary>The targeted recipients of one arm, with their outcomes. A recipient without an outcome row counts as not converted.</summary>
    private static ArmTotals ArmOf(IReadOnlyList<RecipientRow> recipients, string arm, IReadOnlyDictionary<Guid, OutcomeRow> outcomeByRecipient)
    {
        long count = 0;
        long converted = 0;
        decimal revenue = 0m;
        foreach (var recipient in recipients)
        {
            if (recipient.Arm != arm || !recipient.IsTargeted)
            {
                continue;
            }

            count++;
            if (outcomeByRecipient.TryGetValue(recipient.Id, out var outcome))
            {
                converted += outcome.Converted ? 1 : 0;
                revenue += outcome.Revenue;
            }
        }

        return new ArmTotals(new LiftArm(count, converted, (double)revenue), revenue);
    }

    private sealed record TargetedRecipient(Guid RecipientId, Guid ContactId);

    private sealed record RecipientRow(Guid Id, string Arm, bool IsTargeted);

    private sealed record OutcomeRow(Guid RecipientId, bool Converted, decimal Revenue, int SimulatedOrdersInWindow);

    /// <summary>One outcome row as the upsert reads it. The JSON names are snake case, as the statement expects.</summary>
    private sealed record TargetedOutcome(
        Guid Id,
        Guid RecipientId,
        Guid ContactId,
        bool Converted,
        DateTimeOffset? FirstConversionAt,
        decimal Revenue,
        int OrdersInWindow,
        int SimulatedOrdersInWindow);

    private sealed record ArmTotals(LiftArm Arm, decimal Revenue);
}
