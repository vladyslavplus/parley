using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Holdout;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Domain.Targeting;
using Parley.Campaigns.Features.Segments.Shared;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.Crm.Contracts.Audience;
using Parley.Crm.Contracts.Metrics;
using Parley.Insights.Contracts.Scores;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>
/// Freezes the audience of a campaign (domain model 7.2). One run at a time per campaign: the run takes a session
/// advisory lock, and a second run that cannot take it exits. The run then reads the pinned inputs (<c>prepare_as_of</c>),
/// deletes the recipients an earlier run left unfrozen, and writes the audience again from scratch: eligible contacts in
/// batches, minus the suppressed ones, arm and bucket of each, ranks across both arms, and the insert in batches. The
/// commit point is the last statement: it sets <c>audience_frozen_at</c> and moves the campaign to <c>ready</c>. Until
/// then the rows are invisible to sending and to the review.
/// </summary>
public sealed class CampaignAudienceFreezer(
    CampaignsDbContext dbContext,
    ICrmAudienceReader audienceReader,
    IScoreReader scoreReader,
    ITenantContext tenantContext,
    IOptions<CampaignsPrepareOptions> prepareOptions,
    TimeProvider timeProvider)
{
    public async Task FreezeAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        // The session lock belongs to this connection, so the connection stays open for the whole run.
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var acquired = await dbContext.Database
                .SqlQuery<bool>(CampaignAudienceLockSql.TryAcquire(campaignId))
                .SingleAsync(cancellationToken);
            if (!acquired)
            {
                return;
            }

            try
            {
                await FreezeLockedAsync(campaignId, cancellationToken);
            }
            finally
            {
                await dbContext.Database
                    .SqlQuery<bool>(CampaignAudienceLockSql.Release(campaignId))
                    .SingleAsync(CancellationToken.None);
            }
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private async Task FreezeLockedAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var input = await dbContext.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == campaignId)
            .Select(campaign => new CampaignFreezeInput(
                campaign.Id,
                campaign.Status,
                campaign.PrepareAsOf,
                campaign.AudienceFrozenAt,
                campaign.SegmentId,
                campaign.TemplateId,
                campaign.HoldoutSeed,
                campaign.HoldoutShareBps,
                campaign.TargetingMode,
                campaign.TargetingShareBps))
            .FirstOrDefaultAsync(cancellationToken);
        if (input is null || input.AudienceFrozenAt is not null)
        {
            return;
        }

        if (input.Status == CampaignStatuses.Failed)
        {
            await dbContext.Campaigns
                .Where(campaign => campaign.Id == campaignId && campaign.Status == CampaignStatuses.Failed)
                .ExecuteUpdateAsync(setters => setters.SetProperty(campaign => campaign.Status, CampaignStatuses.Preparing), cancellationToken);
        }
        else if (input.Status != CampaignStatuses.Preparing)
        {
            return;
        }

        // A campaign prepared before the as-of instant was pinned gets it now, once; every later run reads the same value.
        var asOfUtc = input.PrepareAsOf ?? timeProvider.GetUtcNow().UtcDateTime;
        if (input.PrepareAsOf is null)
        {
            await dbContext.Campaigns
                .Where(campaign => campaign.Id == campaignId && campaign.PrepareAsOf == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(campaign => campaign.PrepareAsOf, asOfUtc), cancellationToken);
        }

        try
        {
            await WriteRecipientsAsync(input, asOfUtc, cancellationToken);
        }
        catch
        {
            // The rows written so far stay until the next run deletes them. Hangfire retries the job after the failed state is set.
            await MarkFailedAsync(campaignId, CancellationToken.None);
            throw;
        }

        // Commit point: the audience becomes visible to sending only with this statement.
        await dbContext.Campaigns
            .Where(campaign => campaign.Id == campaignId && campaign.Status == CampaignStatuses.Preparing && campaign.AudienceFrozenAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(campaign => campaign.Status, CampaignStatuses.Ready)
                    .SetProperty(campaign => campaign.AudienceFrozenAt, timeProvider.GetUtcNow().UtcDateTime),
                cancellationToken);
    }

    private async Task WriteRecipientsAsync(CampaignFreezeInput input, DateTime asOfUtc, CancellationToken cancellationToken)
    {
        // Rewritten from scratch on every run: rows of an earlier run that never reached the commit point are dropped here,
        // so a resumed or repeated freeze cannot mix two rankings or keep a contact that was suppressed since.
        await dbContext.Recipients
            .Where(recipient => recipient.CampaignId == input.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var definition = await LoadDefinitionAsync(input.SegmentId, cancellationToken);
        var locale = await dbContext.Templates
            .AsNoTracking()
            .Where(template => template.Id == input.TemplateId)
            .Select(template => template.Locale)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The campaign template no longer exists.");

        var scoreKind = CampaignTargetingModes.ScoreKindOf(input.TargetingMode);
        var candidates = new List<CampaignFreezeCandidate>();
        var scores = new Dictionary<Guid, ContactScore>();

        // One RFM day for the whole audience: every batch below reads the same complete day, so the freeze is never half of one day.
        var rfmDay = await scoreReader.ReadLatestRfmDayAsync(cancellationToken);

        Guid? afterContactId = null;
        while (true)
        {
            var batch = await audienceReader.ReadEligibleBatchAsync(afterContactId, CrmCustomerMetricsContract.MaximumBatchSize, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            var matched = (await RfmSegmentFilter.ApplyAsync(
                batch.Where(row => SegmentAudienceRules.Matches(row, definition, asOfUtc)).ToList(),
                definition,
                scoreReader,
                rfmDay,
                cancellationToken)).ToList();
            var suppression = await ReadSuppressionAsync(matched, cancellationToken);
            var eligible = matched
                .Where(row => !suppression.Contacts.Contains(row.ContactId)
                    && (row.EmailNormalized is null || !suppression.Emails.Contains(row.EmailNormalized)))
                .ToList();

            if (scoreKind is not null && eligible.Count > 0)
            {
                var batchScores = await scoreReader.ReadLatestScoresAsync(
                    scoreKind,
                    eligible.Select(row => row.ContactId).ToList(),
                    rfmDay,
                    cancellationToken);
                foreach (var (contactId, score) in batchScores)
                {
                    scores[contactId] = score;
                }
            }

            foreach (var row in eligible)
            {
                var arm = HoldoutAssigner.Assign(input.HoldoutSeed, row.ContactId, input.HoldoutShareBps);
                var bucket = HoldoutAssigner.Bucket(input.HoldoutSeed, row.ContactId);
                candidates.Add(new CampaignFreezeCandidate(row.ContactId, row.Email, arm, bucket));
            }

            afterContactId = batch[^1].ContactId;
            if (batch.Count < CrmCustomerMetricsContract.MaximumBatchSize)
            {
                break;
            }
        }

        // Ranking across both arms, before arm-specific behavior: the same contacts are targeted whatever their arm.
        var rankByContact = new Dictionary<Guid, int>();
        HashSet<Guid>? targeted = null;
        if (scoreKind is not null)
        {
            var scored = candidates
                .Where(candidate => scores.ContainsKey(candidate.ContactId))
                .Select(candidate => (candidate.ContactId, scores[candidate.ContactId].Value))
                .ToList();
            var ordered = TargetingSelector.Order(scored);
            for (var index = 0; index < ordered.Count; index++)
            {
                rankByContact[ordered[index]] = index + 1;
            }

            var targetCount = TargetingSelector.TargetCount(
                candidates.Count,
                input.TargetingShareBps ?? throw new InvalidOperationException("A ranking campaign has no targeting share."));
            targeted = ordered.Take(targetCount).ToHashSet();
        }

        var now = timeProvider.GetUtcNow();
        var rows = candidates.Select(candidate => ToRow(candidate, scoreKind, scores, rankByContact, targeted, locale)).ToList();

        var batchSize = prepareOptions.Value.BatchSize;
        for (var offset = 0; offset < rows.Count; offset += batchSize)
        {
            var size = Math.Min(batchSize, rows.Count - offset);
            var chunk = rows.GetRange(offset, size);
            await dbContext.Database.ExecuteSqlAsync(
                RecipientInsertSql.Insert(tenantContext.OrganizationId, input.Id, now, RecipientInsertSql.Serialize(chunk)),
                cancellationToken);
        }
    }

    private static RecipientInsertRow ToRow(
        CampaignFreezeCandidate candidate,
        string? scoreKind,
        IReadOnlyDictionary<Guid, ContactScore> scores,
        IReadOnlyDictionary<Guid, int> rankByContact,
        IReadOnlySet<Guid>? targeted,
        string locale)
    {
        ContactScore? score = null;
        if (scoreKind is not null && scores.TryGetValue(candidate.ContactId, out var found))
        {
            score = found;
        }

        return new RecipientInsertRow(
            Guid.CreateVersion7(),
            candidate.ContactId,
            candidate.Arm,
            candidate.Bucket,
            scoreKind,
            score?.Value,
            score?.ModelVersion,
            rankByContact.TryGetValue(candidate.ContactId, out var rank) ? rank : null,
            targeted is null || targeted.Contains(candidate.ContactId),
            candidate.Email,
            locale);
    }

    private async Task<SegmentDefinition> LoadDefinitionAsync(Guid? segmentId, CancellationToken cancellationToken)
    {
        if (segmentId is null)
        {
            return new SegmentDefinition();
        }

        var definition = await dbContext.Segments
            .AsNoTracking()
            .Where(segment => segment.Id == segmentId && segment.ArchivedAt == null)
            .Select(segment => segment.Definition)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The campaign segment no longer exists.");
        return SegmentDefinitionRules.Normalize(definition);
    }

    /// <summary>Suppressions that apply to a batch: by contact id, or by normalized address.</summary>
    private async Task<SuppressedAudience> ReadSuppressionAsync(IReadOnlyList<CrmAudienceRow> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return new SuppressedAudience([], []);
        }

        var contactIds = rows.Select(row => row.ContactId).Distinct().ToList();
        var emails = rows
            .Where(row => row.EmailNormalized is not null)
            .Select(row => row.EmailNormalized!)
            .Distinct()
            .ToList();
        var found = await dbContext.Suppressions
            .AsNoTracking()
            .Where(suppression => contactIds.Contains(suppression.ContactId) || emails.Contains(suppression.EmailNormalized))
            .Select(suppression => new { suppression.ContactId, suppression.EmailNormalized })
            .ToListAsync(cancellationToken);
        return new SuppressedAudience(
            found.Select(row => row.ContactId).ToHashSet(),
            found.Select(row => row.EmailNormalized).ToHashSet());
    }

    private async Task MarkFailedAsync(Guid campaignId, CancellationToken cancellationToken) =>
        await dbContext.Campaigns
            .Where(campaign => campaign.Id == campaignId && campaign.Status == CampaignStatuses.Preparing && campaign.AudienceFrozenAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(campaign => campaign.Status, CampaignStatuses.Failed), cancellationToken);

    private sealed record SuppressedAudience(HashSet<Guid> Contacts, HashSet<string> Emails);
}
