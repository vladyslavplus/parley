using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Holdout;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Shared;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Review;

public sealed class ReviewCampaignQueryHandler(CampaignsDbContext dbContext, IOptions<CampaignsAnalysisOptions> analysis)
    : IRequestHandler<ReviewCampaignQuery, Result<CampaignReviewView>>
{
    public async ValueTask<Result<CampaignReviewView>> Handle(ReviewCampaignQuery query, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.Campaigns
            .AsNoTracking()
            .Where(row => row.Id == query.CampaignId)
            .Select(row => new { row.Status, row.AudienceFrozenAt, row.HoldoutShareBps })
            .FirstOrDefaultAsync(cancellationToken);
        if (campaign is null)
        {
            return Result<CampaignReviewView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound));
        }

        // An unfinished freeze is invisible: its partial rows are not an experiment yet, so the review shows nothing for them.
        var arms = campaign.AudienceFrozenAt is null
            ? new List<CampaignArmSummary>()
            : await dbContext.Recipients
                .AsNoTracking()
                .Where(recipient => recipient.CampaignId == query.CampaignId)
                .GroupBy(recipient => recipient.Arm)
                .Select(group => new CampaignArmSummary(group.Key, group.Count(), group.Count(recipient => recipient.IsTargeted)))
                .ToListAsync(cancellationToken);

        var treatment = arms.FirstOrDefault(arm => arm.Arm == HoldoutArms.Treatment) ?? new CampaignArmSummary(HoldoutArms.Treatment, 0, 0);
        var holdout = arms.FirstOrDefault(arm => arm.Arm == HoldoutArms.Holdout) ?? new CampaignArmSummary(HoldoutArms.Holdout, 0, 0);

        double? srmPValue = treatment.Count + holdout.Count == 0
            ? null
            : SampleRatioCheck.PValue(holdout.Count, treatment.Count, campaign.HoldoutShareBps);
        var srmWarning = srmPValue is { } pValue && pValue < analysis.Value.SrmAlarmP;

        int? minimumDetectableEffectBps = null;
        if (treatment.TargetedCount > 0 && holdout.TargetedCount > 0)
        {
            var baselineRate = (double)analysis.Value.PreviewBaselineRateBps / BasisPoints.Scale;
            var effect = MinimumDetectableEffect.Compute(
                treatment.TargetedCount,
                holdout.TargetedCount,
                baselineRate,
                analysis.Value.ZAlpha,
                analysis.Value.ZBeta);
            minimumDetectableEffectBps = (int)Math.Round(effect * BasisPoints.Scale);
        }

        return Result<CampaignReviewView>.Success(new CampaignReviewView(
            query.CampaignId,
            campaign.Status,
            campaign.AudienceFrozenAt,
            campaign.HoldoutShareBps,
            treatment,
            holdout,
            srmPValue,
            srmWarning,
            minimumDetectableEffectBps));
    }
}
