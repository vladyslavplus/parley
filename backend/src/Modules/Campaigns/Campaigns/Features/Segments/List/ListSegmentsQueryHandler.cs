using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Campaigns.Features.Segments.List;

public sealed class ListSegmentsQueryHandler(CampaignsDbContext dbContext)
    : IRequestHandler<ListSegmentsQuery, Result<KeysetCursorPage<SegmentSummaryView>>>
{
    private const string NameSortField = "name";

    public async ValueTask<Result<KeysetCursorPage<SegmentSummaryView>>> Handle(ListSegmentsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Result<KeysetCursorPage<SegmentSummaryView>>.Failure(Error.Validation(ErrorCodes.ValidationFailed));
        }

        var segments = dbContext.Segments.AsNoTracking().Where(segment => segment.ArchivedAt == null);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            segments = segments.Where(segment => EF.Functions.ILike(segment.Name, pattern));
        }

        var page = await KeysetPages.ReadAsync(
            segments,
            cursor,
            NameSortField,
            segment => segment.Name,
            segment => segment.Id,
            segment => new SegmentSummaryView(segment.Id, segment.Name),
            summary => summary.Name,
            summary => summary.Id,
            KeysetOrdering.ParseText,
            KeysetOrdering.FormatText,
            descending: false,
            limit,
            cancellationToken);
        return page is null
            ? Result<KeysetCursorPage<SegmentSummaryView>>.Failure(Error.Validation(ErrorCodes.ValidationFailed))
            : Result<KeysetCursorPage<SegmentSummaryView>>.Success(page);
    }
}
