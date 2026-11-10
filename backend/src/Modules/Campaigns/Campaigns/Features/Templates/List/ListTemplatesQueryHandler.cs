using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Features.Templates.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Campaigns.Features.Templates.List;

public sealed class ListTemplatesQueryHandler(CampaignsDbContext dbContext)
    : IRequestHandler<ListTemplatesQuery, Result<KeysetCursorPage<TemplateSummaryView>>>
{
    private const string NameSortField = "name";

    public async ValueTask<Result<KeysetCursorPage<TemplateSummaryView>>> Handle(ListTemplatesQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Result<KeysetCursorPage<TemplateSummaryView>>.Failure(Error.Validation(ErrorCodes.ValidationFailed));
        }

        var templates = dbContext.Templates.AsNoTracking().Where(template => template.ArchivedAt == null);
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            templates = templates.Where(template => EF.Functions.ILike(template.Name, pattern));
        }

        var page = await KeysetPages.ReadAsync(
            templates,
            cursor,
            NameSortField,
            template => template.Name,
            template => template.Id,
            template => new TemplateSummaryView(template.Id, template.Name, template.Locale),
            summary => summary.Name,
            summary => summary.Id,
            KeysetOrdering.ParseText,
            KeysetOrdering.FormatText,
            descending: false,
            limit,
            cancellationToken);
        return page is null
            ? Result<KeysetCursorPage<TemplateSummaryView>>.Failure(Error.Validation(ErrorCodes.ValidationFailed))
            : Result<KeysetCursorPage<TemplateSummaryView>>.Success(page);
    }
}
