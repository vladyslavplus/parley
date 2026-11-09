using System.Globalization;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Crm.Features.CustomFields.List;

public sealed class ListCustomFieldsQueryHandler(CrmDbContext dbContext)
    : IRequestHandler<ListCustomFieldsQuery, Result<KeysetCursorPage<CustomFieldDefinitionView>>>
{
    public async ValueTask<Result<KeysetCursorPage<CustomFieldDefinitionView>>> Handle(ListCustomFieldsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var definitions = Filter(dbContext.CustomFieldDefinitions.AsNoTracking(), query);
        var descending = SortDirections.IsDescending(query.Order);
        var page = (string.IsNullOrEmpty(query.Sort) ? CustomFieldListRules.SortName : query.Sort) switch
        {
            CustomFieldListRules.SortCreatedAt => await KeysetPages.ReadAsync(
                definitions, cursor, CustomFieldListRules.SortCreatedAt, definition => (DateTime?)definition.CreatedAt, definition => definition.Id,
                definition => definition, definition => (DateTime?)definition.CreatedAt, definition => definition.Id,
                KeysetOrdering.ParseTimestamp, KeysetOrdering.FormatTimestamp, descending, limit, cancellationToken),
            CustomFieldListRules.SortPosition => await KeysetPages.ReadAsync(
                definitions, cursor, CustomFieldListRules.SortPosition, definition => (int?)definition.Position, definition => definition.Id,
                definition => definition, definition => (int?)definition.Position, definition => definition.Id,
                ParsePosition, FormatPosition, descending, limit, cancellationToken),
            _ => await KeysetPages.ReadAsync(
                definitions, cursor, CustomFieldListRules.SortName, definition => definition.Label, definition => definition.Id,
                definition => definition, definition => definition.Label, definition => definition.Id,
                KeysetOrdering.ParseText, KeysetOrdering.FormatText, descending, limit, cancellationToken),
        };

        return page is null
            ? Failure()
            : Result<KeysetCursorPage<CustomFieldDefinitionView>>.Success(
                new KeysetCursorPage<CustomFieldDefinitionView>(page.Items.Select(CustomFieldDefinitionViews.ToView).ToArray(), page.NextCursor));
    }

    private static IQueryable<CustomFieldDefinition> Filter(IQueryable<CustomFieldDefinition> definitions, ListCustomFieldsQuery query)
    {
        if (!string.IsNullOrEmpty(query.EntityType))
        {
            var entityType = query.EntityType;
            definitions = definitions.Where(definition => definition.EntityType == entityType);
        }

        if (!string.IsNullOrEmpty(query.FieldType))
        {
            var fieldType = query.FieldType;
            definitions = definitions.Where(definition => definition.FieldType == fieldType);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            definitions = definitions.Where(definition => EF.Functions.ILike(definition.Label, pattern) || EF.Functions.ILike(definition.Key, pattern));
        }

        return definitions;
    }

    private static int? ParsePosition(string key) =>
        int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var position) ? position : null;

    private static string FormatPosition(int? position) => position?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static Result<KeysetCursorPage<CustomFieldDefinitionView>> Failure() =>
        Result<KeysetCursorPage<CustomFieldDefinitionView>>.Failure(PagingErrors.Field(nameof(ListCustomFieldsQuery.After)));
}
