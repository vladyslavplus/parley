using Mediator;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Segments.Get;

public sealed class GetSegmentQueryHandler(CampaignsDbContext dbContext) : IRequestHandler<GetSegmentQuery, Result<SegmentView>>
{
    public async ValueTask<Result<SegmentView>> Handle(GetSegmentQuery query, CancellationToken cancellationToken)
    {
        var segment = await SegmentLookup.FindLiveViewAsync(dbContext, query.Id, cancellationToken);
        return segment is null
            ? Result<SegmentView>.Failure(Error.NotFound(CampaignsErrorCodes.SegmentNotFound))
            : Result<SegmentView>.Success(segment);
    }
}
