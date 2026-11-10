using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Parley.Campaigns.Contracts.Events;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Features.Campaigns.Dispatch.Claims;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Outbox.Publishing;

namespace Parley.Campaigns.Features.Campaigns.Dispatch.Outcomes;

/// <summary>
/// Records the outcome of one send. Each outcome is one short transaction that moves the claim out of <c>claimed</c>.
/// The update is conditional on <c>claimed</c>, so an outcome that arrives after the lease expired cannot overwrite
/// <c>unknown</c>. The sent event is written to the outbox in the same commit as the status change.
/// </summary>
public sealed class CampaignSendRecorder(
    CampaignsDbContext dbContext,
    IModuleOutbox<CampaignsDbContext> outbox,
    TimeProvider timeProvider,
    ILogger<CampaignSendRecorder> logger)
{
    public Task MarkSentAsync(Guid campaignId, ClaimedSend send, CancellationToken cancellationToken) =>
        RecordAsync(send, cancellationToken, async () =>
        {
            var now = timeProvider.GetUtcNow();
            var updated = await dbContext.Sends
                .Where(row => row.Id == send.SendId && row.Status == SendStatuses.Claimed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(row => row.Status, SendStatuses.Sent)
                    .SetProperty(row => row.SentAt, now.UtcDateTime)
                    .SetProperty(row => row.LeaseExpiresAt, (DateTime?)null)
                    .SetProperty(row => row.ErrorCode, (string?)null)
                    .SetProperty(row => row.UpdatedAt, now.UtcDateTime), cancellationToken);
            if (updated != 1)
            {
                return false;
            }

            outbox.Add(new CampaignEmailSent
            {
                CampaignId = campaignId,
                ContactId = send.ContactId,
                MessageId = send.MessageId,
                SentAt = now,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        });

    /// <summary>A recipient that cannot be sent to at all: no usable address, no live contact, or a template that does not render.</summary>
    public Task MarkFailedAsync(ClaimedSend send, string errorCode, CancellationToken cancellationToken) =>
        SetOutcomeAsync(send, SendStatuses.Failed, errorCode, cancellationToken);

    /// <summary>A connection-level failure before acceptance: retried while attempts remain, failed on the last one.</summary>
    public Task MarkNotAcceptedAsync(ClaimedSend send, int maxAttempts, string errorCode, CancellationToken cancellationToken) =>
        SetOutcomeAsync(send, SendPolicy.AfterNotAccepted(send.AttemptCount, maxAttempts), errorCode, cancellationToken);

    /// <summary>The outcome is not known. The message is never sent again.</summary>
    public Task MarkUnknownAsync(ClaimedSend send, string errorCode, CancellationToken cancellationToken) =>
        SetOutcomeAsync(send, SendStatuses.Unknown, errorCode, cancellationToken);

    private Task SetOutcomeAsync(ClaimedSend send, string status, string errorCode, CancellationToken cancellationToken) =>
        RecordAsync(send, cancellationToken, async () =>
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var updated = await dbContext.Sends
                .Where(row => row.Id == send.SendId && row.Status == SendStatuses.Claimed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(row => row.Status, status)
                    .SetProperty(row => row.ErrorCode, errorCode)
                    .SetProperty(row => row.LeaseExpiresAt, (DateTime?)null)
                    .SetProperty(row => row.UpdatedAt, now), cancellationToken);
            return updated == 1;
        });

    /// <summary>Runs one outcome write in its own transaction and logs when the claim had already left <c>claimed</c>.</summary>
    private async Task RecordAsync(ClaimedSend send, CancellationToken cancellationToken, Func<Task<bool>> write)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var recorded = await write();
        await transaction.CommitAsync(cancellationToken);
        if (!recorded)
        {
            logger.LogWarning(
                "A send outcome arrived after its claim left the claimed state. Claim {SendId} keeps its recorded status.",
                send.SendId);
        }
    }
}
