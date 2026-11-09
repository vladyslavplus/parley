using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Metrics;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Readers;

/// <summary>
/// Reads the customer metrics with one keyset-paged SQL statement per batch. The organization filter is the first
/// predicate of every table access; the statement is defined in <see cref="CustomerMetricsSql"/>.
/// </summary>
public sealed class CrmCustomerMetricsReader(CrmDbContext dbContext, ITenantContext tenantContext) : ICrmCustomerMetricsReader
{
    public async Task<IReadOnlyList<CrmCustomerMetricsRow>> ReadBatchAsync(
        Guid? afterContactId,
        int limit,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > CrmCustomerMetricsContract.MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        // Windows are computed on the UTC instant: an offset in asOf must not shift the calendar arithmetic.
        var instant = asOf.ToUniversalTime();
        var shortWindowStart = instant.AddDays(-CrmCustomerMetricsContract.ShortWindowDays);
        var longWindowStart = instant.AddDays(-CrmCustomerMetricsContract.LongWindowDays);
        var rows = await dbContext.Database
            .SqlQuery<CustomerMetricsSqlRow>(CustomerMetricsSql.Page(
                tenantContext.OrganizationId,
                afterContactId,
                limit,
                instant,
                shortWindowStart,
                longWindowStart,
                DealStatuses.Open))
            .ToListAsync(cancellationToken);

        return rows.Select(ToContract).ToArray();
    }

    private static CrmCustomerMetricsRow ToContract(CustomerMetricsSqlRow row) => new(
        row.ContactId,
        row.OwnerUserId,
        ToOffset(row.FirstOrderAt),
        ToOffset(row.LastOrderAt),
        row.OrderCount,
        row.PurchaseDayCount,
        row.OrdersShort,
        row.OrdersLong,
        row.TotalValue,
        row.ValueLong,
        row.AvgOrderValue,
        row.AvgRepeatOrderValue,
        row.HasOpenDeal);

    private static DateTimeOffset? ToOffset(DateTime? value) =>
        value is { } instant ? new DateTimeOffset(DateTime.SpecifyKind(instant, DateTimeKind.Utc)) : null;
}
