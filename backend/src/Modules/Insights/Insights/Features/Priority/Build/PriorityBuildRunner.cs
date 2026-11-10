using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Crm.Contracts.Priority;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Domain.Rfm;
using Parley.Insights.Features.Daily;
using Parley.Insights.Options;
using Parley.Insights.Persistence;
using Parley.Insights.Shared;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Priority.Build;

/// <summary>
/// Builds the priority list of one organization day from the RFM snapshot of that day and the CRM facts of the contacts.
/// Streams the snapshot in keyset batches, scores each contact with the pure rules, keeps the best entries of each owner,
/// and replaces the rows of the day in one transaction. A repeated build leaves the same rows.
/// </summary>
public sealed class PriorityBuildRunner(
    InsightsDbContext dbContext,
    ICrmPriorityFactsReader facts,
    ITenantContext tenantContext,
    IOptions<InsightsPriorityOptions> options,
    TimeProvider timeProvider)
{
    /// <summary>Builds the list of <paramref name="asOf"/> for the current tenant and returns the number of entries written.</summary>
    public async Task<int> RunAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        var priority = options.Value;
        var instant = timeProvider.GetUtcNow();
        var staleBefore = instant.AddDays(-priority.StaleDealDays);
        var buckets = new PriorityBuckets(priority.ItemsPerOwner);

        Guid? afterContactId = null;
        while (true)
        {
            var batch = await ReadInputBatchAsync(asOf, afterContactId, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            var factRows = await facts.ReadBatchAsync(
                batch.Select(row => row.ContactId).ToList(),
                instant,
                staleBefore,
                cancellationToken);
            var factsByContact = factRows.ToDictionary(row => row.ContactId);

            foreach (var row in batch)
            {
                var contactFacts = factsByContact.TryGetValue(row.ContactId, out var found)
                    ? new PriorityFacts(found.HasDueTask, found.HasStaleDeal)
                    : new PriorityFacts(false, false);
                var tiers = row.RTier is { } recency && row.FTier is { } frequency && row.MTier is { } monetary
                    ? new RfmTiers(recency, frequency, monetary)
                    : (RfmTiers?)null;

                var score = PriorityRules.Score(
                    row.Segment,
                    tiers,
                    contactFacts,
                    priority.Weights,
                    priority.TaskDueBonus,
                    priority.StaleDealBonus);
                if (score.Value <= 0)
                {
                    continue;
                }

                buckets.Add(new PriorityCandidate(
                    row.ContactId,
                    row.OwnerUserId,
                    row.Segment,
                    score.Value,
                    score.ReasonCodes,
                    score.SuggestedAction));
            }

            if (batch.Count < InsightsLimits.PriorityBatchSize)
            {
                break;
            }

            afterContactId = batch[^1].ContactId;
        }

        var organizationId = tenantContext.OrganizationId;
        var items = buckets.Ranked()
            .Select(entry => new PriorityItem
            {
                OrganizationId = organizationId,
                AsOf = asOf,
                OwnerUserId = entry.Candidate.OwnerUserId,
                ContactId = entry.Candidate.ContactId,
                Rank = entry.Rank,
                Mode = PriorityModes.Rfm,
                PriorityValue = entry.Candidate.Value,
                ReasonCodes = entry.Candidate.ReasonCodes.ToArray(),
                SuggestedAction = entry.Candidate.SuggestedAction,
                Segment = entry.Candidate.Segment,
            })
            .ToList();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.PriorityItems.IgnoreQueryFilters()
            .Where(item => item.OrganizationId == organizationId && item.AsOf == asOf)
            .ExecuteDeleteAsync(cancellationToken);
        for (var offset = 0; offset < items.Count; offset += InsightsLimits.PriorityWriteBatchSize)
        {
            dbContext.PriorityItems.AddRange(items.Skip(offset).Take(InsightsLimits.PriorityWriteBatchSize));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        // Only now the list is complete: readers take the priority list of a day once this stamp is set.
        await DailyRunStamps.MarkPriorityCompletedAsync(dbContext, organizationId, asOf, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        return items.Count;
    }

    private async Task<List<PriorityInputRow>> ReadInputBatchAsync(DateOnly asOf, Guid? afterContactId, CancellationToken cancellationToken)
    {
        var snapshots = dbContext.RfmSnapshots.AsNoTracking().Where(snapshot => snapshot.AsOf == asOf);
        if (afterContactId is { } after)
        {
            snapshots = snapshots.Where(snapshot => snapshot.ContactId > after);
        }

        return await snapshots
            .OrderBy(snapshot => snapshot.ContactId)
            .Take(InsightsLimits.PriorityBatchSize)
            .Select(snapshot => new PriorityInputRow(
                snapshot.ContactId,
                snapshot.OwnerUserId,
                snapshot.Segment,
                snapshot.RTier,
                snapshot.FTier,
                snapshot.MTier))
            .ToListAsync(cancellationToken);
    }
}
