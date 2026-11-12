using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Metrics;
using Parley.Insights.Domain.Rfm;
using Parley.Insights.Features.Daily;
using Parley.Insights.Persistence;
using Parley.Insights.Shared;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Rfm.Compute;

/// <summary>
/// Computes the RFM snapshot of one organization day. Reads the customer metrics as of the run instant in keyset batches,
/// keeps one compact input per contact (the quintile ranks need every contact), computes the tiers and segments, and
/// upserts the rows in batches. Idempotent: a repeated run rewrites the same rows, then deletes the rows of the day that
/// this run did not touch, such as a contact deleted since the last run.
/// </summary>
public sealed class RfmSnapshotRunner(
    InsightsDbContext dbContext,
    ICrmCustomerMetricsReader metrics,
    ITenantContext tenantContext,
    TimeProvider timeProvider)
{
    /// <summary>Writes the snapshot of <paramref name="asOf"/> for the current tenant and returns the number of contacts written.</summary>
    public async Task<int> RunAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        var instant = timeProvider.GetUtcNow();
        var stamp = WithMicrosecondPrecision(instant);
        var organizationId = tenantContext.OrganizationId;

        var withOrders = new List<RfmInput>();
        var withoutOrders = new List<(Guid ContactId, Guid? OwnerUserId)>();
        Guid? afterContactId = null;
        while (true)
        {
            var batch = await metrics.ReadBatchAsync(afterContactId, CrmCustomerMetricsContract.MaximumBatchSize, instant, cancellationToken);
            foreach (var row in batch)
            {
                if (row.LastOrderAt is { } lastOrderAt)
                {
                    withOrders.Add(new RfmInput(
                        row.ContactId,
                        row.OwnerUserId,
                        RfmRecency.DaysBetween(instant, lastOrderAt),
                        row.Orders365Days,
                        row.Value365Days));
                }
                else
                {
                    withoutOrders.Add((row.ContactId, row.OwnerUserId));
                }
            }

            if (batch.Count < CrmCustomerMetricsContract.MaximumBatchSize)
            {
                break;
            }

            afterContactId = batch[^1].ContactId;
        }

        var tiers = RfmTierCalculator.Compute(withOrders);
        for (var offset = 0; offset < withOrders.Count; offset += InsightsLimits.RfmWriteBatchSize)
        {
            var rows = withOrders
                .Skip(offset)
                .Take(InsightsLimits.RfmWriteBatchSize)
                .Select(input => ToRow(input, tiers[input.ContactId]))
                .ToList();
            await UpsertAsync(organizationId, stamp, asOf, rows, cancellationToken);
        }

        for (var offset = 0; offset < withoutOrders.Count; offset += InsightsLimits.RfmWriteBatchSize)
        {
            var rows = withoutOrders
                .Skip(offset)
                .Take(InsightsLimits.RfmWriteBatchSize)
                .Select(contact => new RfmSnapshotRow(
                    Guid.CreateVersion7(),
                    contact.ContactId,
                    contact.OwnerUserId,
                    null,
                    0,
                    0m,
                    null,
                    null,
                    null,
                    RfmSegmentRules.Classify(null)))
                .ToList();
            await UpsertAsync(organizationId, stamp, asOf, rows, cancellationToken);
        }

        await dbContext.RfmSnapshots.IgnoreQueryFilters()
            .Where(snapshot => snapshot.OrganizationId == organizationId
                && snapshot.AsOf == asOf
                && snapshot.UpdatedAt < stamp.UtcDateTime)
            .ExecuteDeleteAsync(cancellationToken);

        // Only now the day is complete: readers take the snapshot of a day once this stamp is set.
        await DailyRunStamps.MarkRfmCompletedAsync(dbContext, organizationId, asOf, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return withOrders.Count + withoutOrders.Count;
    }

    private static RfmSnapshotRow ToRow(RfmInput input, RfmTiers tiers) => new(
        Guid.CreateVersion7(),
        input.ContactId,
        input.OwnerUserId,
        input.RecencyDays,
        input.Orders365Days,
        input.Value365Days,
        tiers.R,
        tiers.F,
        tiers.M,
        RfmSegmentRules.Classify(tiers));

    private async Task UpsertAsync(
        Guid organizationId,
        DateTimeOffset stamp,
        DateOnly asOf,
        IReadOnlyList<RfmSnapshotRow> rows,
        CancellationToken cancellationToken) =>
        await dbContext.Database.ExecuteSqlAsync(
            RfmSnapshotSql.Upsert(organizationId, stamp, asOf, RfmSnapshotSql.Serialize(rows)),
            cancellationToken);

    /// <summary>The database stores microseconds, so the stamp is cut to them before it is written and compared.</summary>
    private static DateTimeOffset WithMicrosecondPrecision(DateTimeOffset instant) =>
        new(instant.Ticks - (instant.Ticks % TicksPerMicrosecond), instant.Offset);

    private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
}
