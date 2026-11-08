using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Features.Tags.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tags.List;

public sealed class ListTagsQueryHandler(CrmDbContext dbContext) : IRequestHandler<ListTagsQuery, Result<KeysetCursorPage<TagView>>>
{
    public async ValueTask<Result<KeysetCursorPage<TagView>>> Handle(ListTagsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Result<KeysetCursorPage<TagView>>.Failure(CrmErrors.Field(nameof(ListTagsQuery.After), ErrorCodes.ValidationFailed));
        }

        var tags = dbContext.Tags.AsNoTracking();
        tags = cursor is null
            ? tags.OrderBy(tag => tag.NameNormalized).ThenBy(tag => tag.Id)
            : tags
                .Where(tag => string.Compare(tag.NameNormalized, cursor.SortKey) > 0 || (tag.NameNormalized == cursor.SortKey && tag.Id > cursor.Id))
                .OrderBy(tag => tag.NameNormalized).ThenBy(tag => tag.Id);

        var rows = await tags
            .Select(tag => new TagRow(tag.Id, tag.Name, tag.ColorIndex, tag.NameNormalized))
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        var page = KeysetCursorPage<TagRow>.From(rows, limit, row => new KeysetCursor(row.NameNormalized, row.Id));
        var items = page.Items.Select(TagLookup.ToView).ToArray();
        return Result<KeysetCursorPage<TagView>>.Success(new KeysetCursorPage<TagView>(items, page.NextCursor));
    }
}
