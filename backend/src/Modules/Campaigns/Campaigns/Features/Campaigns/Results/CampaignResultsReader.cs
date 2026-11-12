using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.Results;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Lift;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;

namespace Parley.Campaigns.Features.Campaigns.Results;

/// <summary>
/// Reads the stored result of a campaign through the tenant filter of the context. The window end comes from the campaign:
/// completion plus the window length. Finality comes from the stored figures: they count as final only when they were
/// computed at or after the window end (<see cref="ResultStatus"/>), so the clock alone never makes a claim.
/// </summary>
public sealed class CampaignResultsReader(
    CampaignsDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<CampaignsAnalysisOptions> analysis) : ICampaignResultsReader
{
    public async Task<CampaignResultSummary?> FindLatestCompletedAsync(CancellationToken cancellationToken)
    {
        var latest = await dbContext.CampaignResults
            .AsNoTracking()
            .Join(dbContext.Campaigns.AsNoTracking(), result => result.CampaignId, campaign => campaign.Id, (result, campaign) => new { result, campaign })
            .Where(row => row.campaign.Status == CampaignStatuses.Completed && row.campaign.CompletedAt != null)
            .OrderByDescending(row => row.campaign.CompletedAt)
            .Select(row => new
            {
                row.campaign.Id,
                row.campaign.Name,
                row.campaign.Source,
                row.campaign.CompletedAt,
                row.campaign.AttributionWindowDays,
                row.result.Simulated,
                row.result.SrmPValue,
                row.result.Lift,
                row.result.LiftCiLow,
                row.result.LiftCiHigh,
                row.result.ComputedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is null)
        {
            return null;
        }

        var windowEndsAt = latest.CompletedAt!.Value.AddDays(latest.AttributionWindowDays);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var isFinal = ResultStatus.IsFinal(windowEndsAt, latest.ComputedAt);
        var verdict = LiftVerdict.Decide(isFinal, latest.SrmPValue, analysis.Value.SrmAlarmP, latest.LiftCiLow, latest.LiftCiHigh);
        return new CampaignResultSummary(
            latest.Id,
            latest.Name,
            latest.Source,
            verdict,
            latest.Simulated,
            ResultStatus.WindowState(windowEndsAt, latest.ComputedAt, now),
            isFinal,
            latest.CompletedAt,
            latest.Lift,
            latest.LiftCiLow,
            latest.LiftCiHigh);
    }

    public async Task<CampaignResultsSnapshot?> FindAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.Campaigns
            .AsNoTracking()
            .Where(row => row.Id == campaignId)
            .Select(row => new
            {
                row.Id,
                row.Status,
                row.Source,
                row.StartedAt,
                row.CompletedAt,
                row.AttributionWindowDays,
                row.HoldoutShareBps,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (campaign is null)
        {
            return null;
        }

        var result = await dbContext.CampaignResults
            .AsNoTracking()
            .Where(row => row.CampaignId == campaignId)
            .FirstOrDefaultAsync(cancellationToken);

        var windowEndsAt = campaign.CompletedAt?.AddDays(campaign.AttributionWindowDays);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new CampaignResultsSnapshot(
            campaign.Id,
            campaign.Status,
            campaign.Source,
            campaign.StartedAt,
            campaign.CompletedAt,
            campaign.AttributionWindowDays,
            windowEndsAt,
            ResultStatus.WindowState(windowEndsAt, result?.ComputedAt, now),
            ResultStatus.IsFinal(windowEndsAt, result?.ComputedAt),
            result?.ComputedAt,
            result?.Simulated ?? false,
            campaign.HoldoutShareBps,
            result?.TreatmentAssigned ?? 0,
            result?.HoldoutAssigned ?? 0,
            result?.TreatmentTargeted ?? 0,
            result?.HoldoutTargeted ?? 0,
            result?.TreatmentConverted ?? 0,
            result?.HoldoutConverted ?? 0,
            result?.TreatmentRevenue ?? 0m,
            result?.HoldoutRevenue ?? 0m,
            result?.RateTreatment,
            result?.RateHoldout,
            result?.Lift,
            result?.LiftCiLow,
            result?.LiftCiHigh,
            result?.PValue,
            result?.SrmPValue,
            result?.MinimumDetectableEffect,
            result?.IncrementalRevenue);
    }
}
