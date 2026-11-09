using System.Globalization;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Features.Orders.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Crm.Features.Orders.List;

public sealed class ListOrdersQueryHandler(CrmDbContext dbContext, IDataScope dataScope)
    : IRequestHandler<ListOrdersQuery, Result<KeysetCursorPage<OrderView>>>
{
    public async ValueTask<Result<KeysetCursorPage<OrderView>>> Handle(ListOrdersQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var descending = SortDirections.IsDescending(query.Order, defaultDescending: true);
        var byAmount = query.Sort == OrderSortFields.Amount;

        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var scope = await dataScope.ForPermissionAsync(Permissions.OrdersRead, cancellationToken);
        var visibleContacts = ContactScopeFilter.Apply(dbContext.Contacts.AsNoTracking(), scope);
        if (query.ContactId is { } contactId)
        {
            visibleContacts = visibleContacts.Where(contact => contact.Id == contactId);
        }

        if (query.CompanyId is { } companyId)
        {
            visibleContacts = visibleContacts.Where(contact => contact.CompanyId == companyId);
        }

        var orders = dbContext.Orders.AsNoTracking()
            .Where(order => visibleContacts.Any(contact => contact.Id == order.ContactId));
        if (query.OrderedFrom is { } from)
        {
            var start = from.UtcDateTime;
            orders = orders.Where(order => order.OrderedAt >= start);
        }

        if (query.OrderedTo is { } to)
        {
            var end = to.UtcDateTime;
            orders = orders.Where(order => order.OrderedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search);
            orders = orders.Where(order => EF.Functions.ILike(order.ExternalRef, pattern));
        }

        if (byAmount)
        {
            var position = KeysetOrdering.ReadValuePosition(cursor, OrderSortFields.Amount, ParseAmount);
            if (cursor is not null && position is null)
            {
                return Failure();
            }

            var rows = await KeysetOrdering.Apply(orders, order => order.TotalAmount, order => order.Id, position, descending)
                .Select(OrderViews.RowProjection)
                .Take(limit + 1)
                .ToListAsync(cancellationToken);
            return Page(KeysetCursorPage<OrderRow>.From(rows, limit, row => KeysetOrdering.CursorFor(
                OrderSortFields.Amount,
                row.TotalAmount.ToString(CultureInfo.InvariantCulture),
                row.Id)));
        }

        var orderedPosition = KeysetOrdering.ReadValuePosition(cursor, OrderSortFields.Ordered, KeysetOrdering.ParseTimestamp);
        if (cursor is not null && orderedPosition is null)
        {
            return Failure();
        }

        var orderedRows = await KeysetOrdering.Apply(orders, order => order.OrderedAt, order => order.Id, orderedPosition, descending)
            .Select(OrderViews.RowProjection)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);
        return Page(KeysetCursorPage<OrderRow>.From(orderedRows, limit, row => KeysetOrdering.CursorFor(
            OrderSortFields.Ordered,
            KeysetOrdering.FormatTimestamp(row.OrderedAt),
            row.Id)));
    }

    private static decimal? ParseAmount(string key) =>
        decimal.TryParse(key, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : null;

    private static Result<KeysetCursorPage<OrderView>> Page(KeysetCursorPage<OrderRow> page) =>
        Result<KeysetCursorPage<OrderView>>.Success(new KeysetCursorPage<OrderView>(
            page.Items.Select(OrderViews.ToView).ToArray(),
            page.NextCursor));

    private static Result<KeysetCursorPage<OrderView>> Failure() =>
        Result<KeysetCursorPage<OrderView>>.Failure(CrmErrors.Field(nameof(ListOrdersQuery.After), ErrorCodes.ValidationFailed));
}
