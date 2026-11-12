using Mediator;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Shared;
using Parley.Crm.Contracts.Audience;
using Parley.Crm.Contracts.Metrics;
using Parley.Insights.Contracts.Scores;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Preview;

/// <summary>
/// Counts with <see cref="SegmentAudienceRules"/>, the same rule the audience freeze uses, and with the RFM filter of the
/// score reader when the definition has one. An RFM filter before any RFM tier exists is reported with its own code.
/// </summary>
public sealed class PreviewSegmentQueryHandler(ICrmAudienceReader audienceReader, IScoreReader scores, TimeProvider timeProvider)
    : IRequestHandler<PreviewSegmentQuery, Result<SegmentPreviewView>>
{
    public async ValueTask<Result<SegmentPreviewView>> Handle(PreviewSegmentQuery query, CancellationToken cancellationToken)
    {
        var definition = SegmentDefinitionRules.Normalize(query.Definition);
        if (SegmentAudienceRules.NeedsRfm(definition) && !await scores.HasScoresAsync(ScoreKinds.Rfm, cancellationToken))
        {
            return Result<SegmentPreviewView>.Failure(Error.Validation(CampaignsErrorCodes.SegmentRfmNotAvailable));
        }

        var asOfUtc = timeProvider.GetUtcNow().UtcDateTime;
        var rfmDay = await scores.ReadLatestRfmDayAsync(cancellationToken);
        var count = 0;
        var sample = new List<Guid>(CampaignsLimits.SegmentPreviewSampleSize);
        Guid? afterContactId = null;
        while (true)
        {
            var batch = await audienceReader.ReadEligibleBatchAsync(afterContactId, CrmCustomerMetricsContract.MaximumBatchSize, cancellationToken);
            var matched = await RfmSegmentFilter.ApplyAsync(
                batch.Where(row => SegmentAudienceRules.Matches(row, definition, asOfUtc)).ToList(),
                definition,
                scores,
                rfmDay,
                cancellationToken);
            foreach (var row in matched)
            {
                count++;
                if (sample.Count < CampaignsLimits.SegmentPreviewSampleSize)
                {
                    sample.Add(row.ContactId);
                }
            }

            if (batch.Count < CrmCustomerMetricsContract.MaximumBatchSize)
            {
                break;
            }

            afterContactId = batch[^1].ContactId;
        }

        return Result<SegmentPreviewView>.Success(new SegmentPreviewView(count, sample));
    }
}
