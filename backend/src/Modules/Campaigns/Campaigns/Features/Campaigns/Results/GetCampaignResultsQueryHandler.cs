using Mediator;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Contracts.Results;
using Parley.Campaigns.Domain.Lift;
using Parley.Campaigns.Options;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Results;

/// <summary>
/// Maps the stored result to the view and decides the verdict. The verdict and the final flag come from the stored figures
/// (final only once computed at or after the window end), so a result is never claimed from provisional data. The window
/// state comes from the reader: <c>awaiting_final</c> means the window ended and the final figures are still to come.
/// </summary>
public sealed class GetCampaignResultsQueryHandler(ICampaignResultsReader reader, IOptions<CampaignsAnalysisOptions> analysis)
    : IRequestHandler<GetCampaignResultsQuery, Result<CampaignResultsView>>
{
    public async ValueTask<Result<CampaignResultsView>> Handle(GetCampaignResultsQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await reader.FindAsync(query.Id, cancellationToken);
        if (snapshot is null)
        {
            return Result<CampaignResultsView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound));
        }

        var verdict = LiftVerdict.Decide(snapshot.IsFinal, snapshot.SrmPValue, analysis.Value.SrmAlarmP, snapshot.LiftCiLow, snapshot.LiftCiHigh);

        return Result<CampaignResultsView>.Success(new CampaignResultsView(
            snapshot.CampaignId,
            snapshot.Status,
            snapshot.Source,
            snapshot.WindowState,
            snapshot.IsFinal,
            snapshot.StartedAt,
            snapshot.CompletedAt,
            snapshot.AttributionWindowDays,
            snapshot.WindowEndsAt,
            snapshot.ComputedAt,
            snapshot.Simulated,
            verdict,
            snapshot.SrmPValue is { } srm && srm < analysis.Value.SrmAlarmP,
            snapshot.HoldoutShareBps,
            new CampaignResultsArmView(
                snapshot.TreatmentAssigned,
                snapshot.TreatmentTargeted,
                snapshot.TreatmentConverted,
                snapshot.RateTreatment,
                snapshot.TreatmentRevenue),
            new CampaignResultsArmView(
                snapshot.HoldoutAssigned,
                snapshot.HoldoutTargeted,
                snapshot.HoldoutConverted,
                snapshot.RateHoldout,
                snapshot.HoldoutRevenue),
            snapshot.Lift,
            snapshot.LiftCiLow,
            snapshot.LiftCiHigh,
            snapshot.PValue,
            snapshot.SrmPValue,
            snapshot.MinimumDetectableEffect,
            snapshot.IncrementalRevenue));
    }
}
