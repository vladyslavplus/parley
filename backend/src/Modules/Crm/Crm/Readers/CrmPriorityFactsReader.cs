using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Metrics;
using Parley.Crm.Contracts.Priority;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Persistence;

namespace Parley.Crm.Readers;

/// <summary>
/// Reads the due-task and stale-deal facts of a batch of contacts with two queries over the live rows of the tenant. The
/// tenant filter of the context is the first predicate; the batch is bounded by the CRM batch maximum.
/// </summary>
public sealed class CrmPriorityFactsReader(CrmDbContext dbContext) : ICrmPriorityFactsReader
{
    public async Task<IReadOnlyList<CrmPriorityFactsRow>> ReadBatchAsync(
        IReadOnlyCollection<Guid> contactIds,
        DateTimeOffset dueAt,
        DateTimeOffset staleBefore,
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

        var ids = contactIds.ToArray();
        var dueInstant = dueAt.UtcDateTime;
        var staleInstant = staleBefore.UtcDateTime;
        var withDueTask = await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.DeletedAt == null && task.CompletedAt == null && task.DueAt != null && task.DueAt <= dueInstant
                && task.ContactId != null && ids.Contains(task.ContactId.Value))
            .Select(task => task.ContactId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var withStaleDeal = await dbContext.Deals
            .AsNoTracking()
            .Where(deal => deal.DeletedAt == null && deal.Status == DealStatuses.Open && deal.UpdatedAt < staleInstant
                && deal.ContactId != null && ids.Contains(deal.ContactId.Value))
            .Select(deal => deal.ContactId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var dueSet = withDueTask.ToHashSet();
        var staleSet = withStaleDeal.ToHashSet();
        return ids
            .Where(id => dueSet.Contains(id) || staleSet.Contains(id))
            .Select(id => new CrmPriorityFactsRow(id, dueSet.Contains(id), staleSet.Contains(id)))
            .ToArray();
    }
}
