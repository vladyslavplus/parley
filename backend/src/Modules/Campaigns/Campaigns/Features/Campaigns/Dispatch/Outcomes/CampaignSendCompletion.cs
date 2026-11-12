using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.Events;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Features.Campaigns.Attribution;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Dispatch.Outcomes;

/// <summary>
/// Moves a sending campaign to <c>completed</c> once every targeted treatment recipient has a final outcome. A recipient
/// is outstanding while it has no claim, while its claim is held, or while a retry is still due. The status change is
/// conditional on <c>sending</c>, so two workers that finish together complete the campaign once. On completion it also
/// queues the attribution run that computes the final figures when the window closes.
/// </summary>
public sealed class CampaignSendCompletion(
    CampaignsDbContext dbContext,
    IModuleOutbox<CampaignsDbContext> outbox,
    IOptions<CampaignsSendOptions> options,
    TimeProvider timeProvider,
    ITenantContext tenantContext,
    CampaignAttributionScheduler attributionScheduler)
{
    private readonly int _maxAttempts = options.Value.MaxAttempts;

    /// <summary>Completes the campaign when nothing is outstanding. Returns whether this call completed it.</summary>
    public async Task<bool> TryCompleteAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var unclaimed = await dbContext.Recipients
            .AsNoTracking()
            .AnyAsync(recipient => recipient.CampaignId == campaignId
                && recipient.Arm == HoldoutArms.Treatment
                && recipient.IsTargeted
                && !dbContext.Sends.Any(send => send.RecipientId == recipient.Id), cancellationToken);
        var inFlight = await dbContext.Sends
            .AsNoTracking()
            .AnyAsync(send => send.CampaignId == campaignId
                && (send.Status == SendStatuses.Claimed
                    || (send.Status == SendStatuses.RetryableFailed && send.AttemptCount < _maxAttempts)), cancellationToken);
        if (unclaimed || inFlight)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        var now = timeProvider.GetUtcNow();
        var completed = await dbContext.Campaigns
            .Where(campaign => campaign.Id == campaignId && campaign.Status == CampaignStatuses.Sending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(campaign => campaign.Status, CampaignStatuses.Completed)
                .SetProperty(campaign => campaign.CompletedAt, now.UtcDateTime)
                .SetProperty(campaign => campaign.UpdatedAt, now.UtcDateTime), cancellationToken);
        if (completed == 1)
        {
            var starter = await dbContext.Campaigns
                .AsNoTracking()
                .Where(campaign => campaign.Id == campaignId)
                .Select(campaign => campaign.StartedBy)
                .FirstAsync(cancellationToken);
            outbox.Add(new CampaignCompleted { CampaignId = campaignId, CompletedAt = now, StartedByUserId = starter });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        if (completed == 1)
        {
            // Queued after the commit: the run finds the completed campaign and stamps final figures at the window end.
            var windowDays = await dbContext.Campaigns
                .AsNoTracking()
                .Where(campaign => campaign.Id == campaignId)
                .Select(campaign => campaign.AttributionWindowDays)
                .FirstAsync(cancellationToken);
            attributionScheduler.ScheduleFinal(tenantContext.OrganizationId, campaignId, now.AddDays(windowDays));
        }

        return completed == 1;
    }
}
