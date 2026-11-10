using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Metrics;
using Parley.Crm.Contracts.Outcomes;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Orders;
using Parley.Crm.Persistence;

namespace Parley.Crm.Readers;

/// <summary>
/// Purchases and won deals of a set of contacts inside one window. Written with LINQ so the organization filter is the
/// global tenant filter; the contact list is a <see cref="List{T}"/>, which EF translates to a parameter array.
/// </summary>
public sealed class CrmOutcomeReader(CrmDbContext dbContext) : ICrmOutcomeReader
{
    public async Task<IReadOnlyList<CrmOutcomeRow>> ReadWindowAsync(
        IReadOnlyList<Guid> contactIds,
        DateTimeOffset windowStartExclusive,
        DateTimeOffset windowEndInclusive,
        CancellationToken cancellationToken)
    {
        if (contactIds.Count > CrmCustomerMetricsContract.MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(contactIds));
        }

        if (contactIds.Count == 0)
        {
            return [];
        }

        var ids = contactIds.ToList();
        var start = windowStartExclusive.UtcDateTime;
        var end = windowEndInclusive.UtcDateTime;

        var purchases = await dbContext.Orders.AsNoTracking()
            .Where(order => ids.Contains(order.ContactId) && order.OrderedAt > start && order.OrderedAt <= end)
            .GroupBy(order => order.ContactId)
            .Select(group => new PurchaseGroup(
                group.Key,
                group.Sum(order => order.IsCancellation ? 0 : 1),
                group.Sum(order => order.TotalAmount),
                group.Min(order => order.IsCancellation ? (DateTime?)null : order.OrderedAt),
                group.Sum(order => !order.IsCancellation && order.Source == OrderSources.DemoSimulated ? 1 : 0)))
            .ToListAsync(cancellationToken);

        var wonDeals = await dbContext.Deals.AsNoTracking()
            .Where(deal => deal.ContactId != null
                && ids.Contains(deal.ContactId.Value)
                && deal.DeletedAt == null
                && deal.Status == DealStatuses.Won
                && deal.ClosedAt > start
                && deal.ClosedAt <= end)
            .GroupBy(deal => deal.ContactId!.Value)
            .Select(group => new WonGroup(group.Key, group.Min(deal => deal.ClosedAt)))
            .ToListAsync(cancellationToken);

        var won = wonDeals.ToDictionary(group => group.ContactId, group => group.FirstWonAt);
        var rows = new List<CrmOutcomeRow>(purchases.Count + won.Count);
        foreach (var purchase in purchases)
        {
            rows.Add(new CrmOutcomeRow(
                purchase.ContactId,
                purchase.PurchaseCount,
                purchase.Revenue,
                ToOffset(purchase.FirstPurchaseAt),
                ToOffset(won.GetValueOrDefault(purchase.ContactId)),
                purchase.SimulatedPurchaseCount));
            won.Remove(purchase.ContactId);
        }

        foreach (var (contactId, firstWonAt) in won)
        {
            rows.Add(new CrmOutcomeRow(contactId, 0, 0m, null, ToOffset(firstWonAt)));
        }

        return rows;
    }

    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value is { } instant ? new DateTimeOffset(DateTime.SpecifyKind(instant, DateTimeKind.Utc)) : null;

    private sealed record PurchaseGroup(Guid ContactId, int PurchaseCount, decimal Revenue, DateTime? FirstPurchaseAt, int SimulatedPurchaseCount);

    private sealed record WonGroup(Guid ContactId, DateTime? FirstWonAt);
}
