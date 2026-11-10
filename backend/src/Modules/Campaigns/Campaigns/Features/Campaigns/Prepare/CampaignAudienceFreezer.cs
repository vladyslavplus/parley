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
/// Freezes the audience of a campaign (domain model 7.2). Pass one reads the eligible contacts in batches, drops the
/// suppressed ones, assigns the arm of each, and reads the scores. Ranking then runs across both arms, so
/// <c>is_targeted</c> is known before any arm-specific behavior. Pass two inserts the recipients in batches with
/// <c>ON CONFLICT DO NOTHING</c>. The commit point is the last statement: it sets <c>audience_frozen_at</c> and moves
/// the campaign to <c>ready</c>. Until then the rows are invisible to sending, and a failed run is resumed by running
/// the freeze again.
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
        var input = await dbContext.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == campaignId)
            .Select(campaign => new CampaignFreezeInput(
                campaign.Id,
                campaign.Status,
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

        try
        {
            await WriteRecipientsAsync(input, cancellationToken);
        }
        catch
        {
            // The rows already written stay: a resumed run keeps them. Hangfire retries the job after the failed state is set.
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

    private async Task WriteRecipientsAsync(CampaignFreezeInput input, CancellationToken cancellationToken)
    {
        var definition = await LoadDefinitionAsync(input.SegmentId, cancellationToken);
        var locale = await dbContext.Templates
            .AsNoTracking()
            .Where(template => template.Id == input.TemplateId)
            .Select(template => template.Locale)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The campaign template no longer exists.");

        var asOfUtc = timeProvider.GetUtcNow().UtcDateTime;
        var scoreKind = CampaignTargetingModes.ScoreKindOf(input.TargetingMode);
        var candidates = new List<CampaignFreezeCandidate>();
        var scores = new Dictionary<Guid, ContactScore>();

        Guid? afterContactId = null;
        while (true)
        {
            var batch = await audienceReader.ReadEligibleBatchAsync(afterContactId, CrmCustomerMetricsContract.MaximumBatchSize, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            var matched = batch.Where(row => SegmentAudienceRules.Matches(row, definition, asOfUtc)).ToList();
            var suppressed = await SuppressedEmailsAsync(matched, cancellationToken);
            var eligible = matched
                .Where(row => row.EmailNormalized is null || !suppressed.Contains(row.EmailNormalized))
                .ToList();

            if (scoreKind is not null && eligible.Count > 0)
            {
                var batchScores = await scoreReader.ReadLatestScoresAsync(
                    scoreKind,
                    eligible.Select(row => row.ContactId).ToList(),
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

    private async Task<HashSet<string>> SuppressedEmailsAsync(IReadOnlyList<CrmAudienceRow> rows, CancellationToken cancellationToken)
    {
        var emails = rows
            .Where(row => row.EmailNormalized is not null)
            .Select(row => row.EmailNormalized!)
            .Distinct()
            .ToList();
        if (emails.Count == 0)
        {
            return [];
        }

        var found = await dbContext.Suppressions
            .AsNoTracking()
            .Where(suppression => emails.Contains(suppression.EmailNormalized))
            .Select(suppression => suppression.EmailNormalized)
            .ToListAsync(cancellationToken);
        return found.ToHashSet();
    }

    private async Task MarkFailedAsync(Guid campaignId, CancellationToken cancellationToken) =>
        await dbContext.Campaigns
            .Where(campaign => campaign.Id == campaignId && campaign.Status == CampaignStatuses.Preparing && campaign.AudienceFrozenAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(campaign => campaign.Status, CampaignStatuses.Failed), cancellationToken);
}
