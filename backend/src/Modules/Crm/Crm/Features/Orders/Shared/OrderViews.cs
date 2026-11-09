using System.Linq.Expressions;
using Parley.Crm.Domain.Orders;

namespace Parley.Crm.Features.Orders.Shared;

/// <summary>Projection and mapping of orders for the read endpoints.</summary>
public static class OrderViews
{
    public static Expression<Func<Order, OrderRow>> RowProjection { get; } = order => new OrderRow
    {
        Id = order.Id,
        ContactId = order.ContactId,
        ExternalRef = order.ExternalRef,
        OrderedAt = order.OrderedAt,
        TotalAmount = order.TotalAmount,
        Currency = order.Currency,
        LineCount = order.LineCount,
        IsCancellation = order.IsCancellation,
        Source = order.Source,
    };

    public static OrderView ToView(OrderRow row) => new(
        row.Id,
        row.ContactId,
        row.ExternalRef,
        new DateTimeOffset(DateTime.SpecifyKind(row.OrderedAt, DateTimeKind.Utc)),
        row.TotalAmount,
        row.Currency,
        row.LineCount,
        row.IsCancellation,
        row.Source);
}
