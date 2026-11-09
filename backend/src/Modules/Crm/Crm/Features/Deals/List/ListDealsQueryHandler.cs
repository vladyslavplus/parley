using System.Linq.Expressions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Deals;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Deals.List;

public sealed class ListDealsQueryHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory) : IRequestHandler<ListDealsQuery, Result<KeysetCursorPage<DealView>>>
{
    public async ValueTask<Result<KeysetCursorPage<DealView>>> Handle(ListDealsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var descending = SortDirections.IsDescending(query.Order);
        var sort = query.Sort ?? DealSortFields.Name;

        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var scope = await dataScope.ForPermissionAsync(Permissions.DealsRead, cancellationToken);
        var visibleContacts = await DealLookup.VisibleContactsAsync(dbContext, dataScope, cancellationToken);
        var visibleCompanies = await DealLookup.VisibleCompaniesAsync(dbContext, dataScope, cancellationToken);
        var filter = new DealListFilter(query.Search, query.StageId, query.OwnerUserId, query.CompanyId, query.ContactId, query.Status);
        var deals = DealListFilters.Apply(DealScopeFilter.Apply(dbContext.Deals.AsNoTracking(), scope), filter);
        var projection = DealViews.RowProjectionFor(visibleContacts, visibleCompanies, dbContext.PipelineStages.AsNoTracking());

        return sort switch
        {
            DealSortFields.Amount => await PageAsync(
                deals, deal => deal.Amount, projection, cursor, DealSortFields.Amount, DealSortKeys.ParseAmount,
                row => row.Amount, DealSortKeys.FormatAmount, descending, limit, cancellationToken),
            DealSortFields.Created => await PageAsync(
                deals, deal => deal.CreatedAt, projection, cursor, DealSortFields.Created, KeysetOrdering.ParseTimestamp,
                row => row.CreatedAt, KeysetOrdering.FormatTimestamp, descending, limit, cancellationToken),
            DealSortFields.Position => await PageAsync(
                deals, deal => deal.BoardPosition, projection, cursor, DealSortFields.Position, DealSortKeys.ParsePosition,
                row => row.BoardPosition, DealSortKeys.FormatPosition, descending, limit, cancellationToken),
            _ => await PageByNameAsync(deals, projection, cursor, descending, limit, cancellationToken),
        };
    }

    /// <summary>A page ordered by a value-type key: amount, creation instant or board position.</summary>
    private async Task<Result<KeysetCursorPage<DealView>>> PageAsync<TKey>(
        IQueryable<Deal> deals,
        Expression<Func<Deal, TKey>> key,
        Expression<Func<Deal, DealRow>> projection,
        KeysetCursor? cursor,
        string sortField,
        Func<string, TKey?> parseKey,
        Func<DealRow, TKey> keyOf,
        Func<TKey, string> formatKey,
        bool descending,
        int limit,
        CancellationToken cancellationToken)
        where TKey : struct
    {
        var position = KeysetOrdering.ReadValuePosition(cursor, sortField, parseKey);
        if (cursor is not null && position is null)
        {
            return Failure();
        }

        var rows = await KeysetOrdering.Apply<Deal, TKey>(deals, key, deal => deal.Id, position, descending)
            .Select(projection)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        return await ToPageAsync(rows, limit, row => KeysetOrdering.CursorFor(sortField, formatKey(keyOf(row)), row.Id), cancellationToken);
    }
    /// <summary>A page ordered by the normalized title, the default sort.</summary>
    private async Task<Result<KeysetCursorPage<DealView>>> PageByNameAsync(
        IQueryable<Deal> deals,
        Expression<Func<Deal, DealRow>> projection,
        KeysetCursor? cursor,
        bool descending,
        int limit,
        CancellationToken cancellationToken)
    {
        var position = cursor is null ? null : KeysetOrdering.ReadPosition(cursor, DealSortFields.Name, KeysetOrdering.ParseText);
        if (cursor is not null && position is null)
        {
            return Failure();
        }

        var rows = await KeysetOrdering.Apply(deals, deal => deal.TitleNormalized, deal => deal.Id, position, descending)
            .Select(projection)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        return await ToPageAsync(rows, limit, row => KeysetOrdering.CursorFor(DealSortFields.Name, row.TitleNormalized, row.Id), cancellationToken);
    }

    private async Task<Result<KeysetCursorPage<DealView>>> ToPageAsync(
        List<DealRow> rows,
        int limit,
        Func<DealRow, KeysetCursor> cursorOf,
        CancellationToken cancellationToken)
    {
        var page = KeysetCursorPage<DealRow>.From(rows, limit, cursorOf);
        var owners = await ownerDirectory.ReadAsync(page.Items.Select(row => (Guid?)row.OwnerUserId), cancellationToken);
        var items = page.Items.Select(row => DealViews.ToView(row, owners)).ToArray();
        return Result<KeysetCursorPage<DealView>>.Success(new KeysetCursorPage<DealView>(items, page.NextCursor));
    }

    private static Result<KeysetCursorPage<DealView>> Failure() =>
        Result<KeysetCursorPage<DealView>>.Failure(CrmErrors.Field(nameof(ListDealsQuery.After), ErrorCodes.ValidationFailed));
}
