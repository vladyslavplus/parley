using Mediator;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Shared;
using Parley.Crm.Contracts.Audience;
using Parley.Crm.Contracts.Metrics;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Preview;

/// <summary>
/// Counts with <see cref="SegmentAudienceRules"/>, the same rule the audience freeze uses. A definition with an RFM
/// filter is reported with its own code instead of a partial count, because the insights data does not exist yet.
/// </summary>
public sealed class PreviewSegmentQueryHandler(ICrmAudienceReader audienceReader, TimeProvider timeProvider)
    : IRequestHandler<PreviewSegmentQuery, Result<SegmentPreviewView>>
{
    public async ValueTask<Result<SegmentPreviewView>> Handle(PreviewSegmentQuery query, CancellationToken cancellationToken)
    {
        var definition = SegmentDefinitionRules.Normalize(query.Definition);
        if (SegmentAudienceRules.NeedsRfm(definition))
        {
            return Result<SegmentPreviewView>.Failure(Error.Validation(CampaignsErrorCodes.SegmentRfmNotAvailable));
        }

        var asOfUtc = timeProvider.GetUtcNow().UtcDateTime;
        var count = 0;
        var sample = new List<Guid>(CampaignsLimits.SegmentPreviewSampleSize);
        Guid? afterContactId = null;
        while (true)
        {
            var batch = await audienceReader.ReadEligibleBatchAsync(afterContactId, CrmCustomerMetricsContract.MaximumBatchSize, cancellationToken);
            foreach (var row in batch)
            {
                if (!SegmentAudienceRules.Matches(row, definition, asOfUtc))
                {
                    continue;
                }

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
