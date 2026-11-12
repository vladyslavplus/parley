using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Crm.Contracts.Priority;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Domain.Rfm;
using Parley.Insights.Features.Daily;
using Parley.Insights.Features.Priority.Shared;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Options;
using Parley.Insights.Persistence;
using Parley.Insights.Shared;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Priority.Build;

/// <summary>
/// Builds the priority list of one organization day from the RFM snapshot of that day, the CRM facts of the contacts and,
/// when the ML scores are usable, the uplift of each contact. A contact with an uplift score is ranked in the ML mode; a
/// contact without one uses the same ML scale with a zero uplift and keeps the RFM label, so a partly scored base still gets
/// a full list. A contact the uplift model marks do-not-disturb stays on the list, ranked last, with a caution reason.
/// Streams the snapshot in keyset batches, keeps the best entries of each owner, and replaces the rows of the day in one
/// transaction. A repeated build leaves the same rows.
/// </summary>
public sealed class PriorityBuildRunner(
    InsightsDbContext dbContext,
    ICrmPriorityFactsReader facts,
    IScoreReader scores,
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
        var requestedStatus = await PriorityMlStatusResolver.ReadAsync(dbContext, cancellationToken);
        // ML ranks only from the run of the version in use. Its id pins the scores, so older runs' rows never mix in.
        var upliftRunId = requestedStatus == PriorityMlStatus.Ready
            ? await ActiveScoringRuns.FindActiveRunIdAsync(dbContext, ScoreKinds.Uplift, cancellationToken)
            : null;
        var mlMode = upliftRunId is not null;
        var mlOptions = new PriorityMlOptions(
            priority.UpliftWeight,
            priority.RfmWeight,
            priority.UpliftHighThreshold,
            priority.Weights,
            priority.TaskDueBonus,
            priority.StaleDealBonus);

        Guid? afterContactId = null;
        while (true)
        {
            var batch = await ReadInputBatchAsync(asOf, afterContactId, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            var contactIds = batch.Select(row => row.ContactId).ToList();
            var factRows = await facts.ReadBatchAsync(contactIds, instant, staleBefore, cancellationToken);
            var factsByContact = factRows.ToDictionary(row => row.ContactId);
            var upliftByContact = mlMode
                ? await scores.ReadLatestScoresAsync(ScoreKinds.Uplift, contactIds, null, upliftRunId, cancellationToken)
                : new Dictionary<Guid, ContactScore>();

            foreach (var row in batch)
            {
                var contactFacts = factsByContact.TryGetValue(row.ContactId, out var found)
                    ? new PriorityFacts(found.HasDueTask, found.HasStaleDeal)
                    : new PriorityFacts(false, false);
                var tiers = row.RTier is { } recency && row.FTier is { } frequency && row.MTier is { } monetary
                    ? new RfmTiers(recency, frequency, monetary)
                    : (RfmTiers?)null;

                if (mlMode)
                {
                    if (upliftByContact.TryGetValue(row.ContactId, out var uplift))
                    {
                        var mlScore = PriorityRules.ScoreMl(
                            row.Segment,
                            tiers,
                            contactFacts,
                            uplift.Value,
                            uplift.Segment ?? string.Empty,
                            mlOptions);
                        AddIfRankable(buckets, row, mlScore, PriorityModes.Ml);
                        continue;
                    }

                    // The same ML scale with a zero uplift: a contact without a score never outranks a scored one by mode.
                    var unscored = PriorityRules.ScoreMl(
                        row.Segment,
                        tiers,
                        contactFacts,
                        0m,
                        string.Empty,
                        mlOptions);
                    AddIfRankable(buckets, row, unscored, PriorityModes.Rfm);
                    continue;
                }

                var score = PriorityRules.Score(
                    row.Segment,
                    tiers,
                    contactFacts,
                    priority.Weights,
                    priority.TaskDueBonus,
                    priority.StaleDealBonus);
                AddIfRankable(buckets, row, score, PriorityModes.Rfm);
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
                Mode = entry.Candidate.Mode,
                PriorityValue = entry.Candidate.Value,
                ReasonCodes = entry.Candidate.ReasonCodes.ToArray(),
                SuggestedAction = entry.Candidate.SuggestedAction,
                Segment = entry.Candidate.Segment,
            })
            .ToList();

        // The stamp describes the rows written, so a reader never sees a mode the rows do not carry. ML is reported only when
        // at least one row is ranked by uplift; an ML-ready switch with no uplift-ranked row reports no scores.
        var rowsMode = items.Any(item => item.Mode == PriorityModes.Ml) ? PriorityModes.Ml : PriorityModes.Rfm;
        var mlStatus = rowsMode == PriorityModes.Ml
            ? PriorityMlStatus.Ready
            : requestedStatus == PriorityMlStatus.Ready ? PriorityMlStatus.NoScores : requestedStatus;

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
        await DailyRunStamps.MarkPriorityCompletedAsync(
            dbContext,
            organizationId,
            asOf,
            timeProvider.GetUtcNow().UtcDateTime,
            rowsMode,
            mlStatus,
            cancellationToken);
        return items.Count;
    }

    /// <summary>
    /// Adds a candidate when it ranks: a positive value, or a do-not-disturb caution that the list keeps at the bottom so the
    /// user sees why the contact should not be contacted now.
    /// </summary>
    private static void AddIfRankable(PriorityBuckets buckets, PriorityInputRow row, PriorityScore score, string mode)
    {
        if (score.Value <= 0 && !score.ReasonCodes.Contains(PriorityReasons.DoNotDisturb))
        {
            return;
        }

        buckets.Add(new PriorityCandidate(
            row.ContactId,
            row.OwnerUserId,
            row.Segment,
            score.Value,
            score.ReasonCodes,
            score.SuggestedAction,
            mode));
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
