using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Domain.Suppressions;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Dispatch.Claims;

/// <summary>
/// Claims the next batch of sends and expires leases. A claim is a row in <c>send_records</c> written before any mail
/// leaves; the unique key on campaign and contact makes a second claim impossible. A recipient whose address was
/// suppressed after the freeze is recorded as <c>skipped_suppressed</c> in the same transaction, so it is never sent.
/// </summary>
public sealed class CampaignSendClaimer(
    CampaignsDbContext dbContext,
    ITenantContext tenantContext,
    IOptions<CampaignsSendOptions> options,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly CampaignsSendOptions _options = options.Value;

    /// <summary>
    /// Resolves every claim whose lease has ended. A claim stamped as submitted may or may not have reached the server, so it
    /// becomes <c>unknown</c> and is never sent again. A claim without the stamp was never handed to the server: while
    /// attempts remain it goes back to the retry pool due at once, and the last attempt makes it failed with a stable code,
    /// so a never-submitted claim can never stay in limbo.
    /// </summary>
    public async Task ExpireLeasesAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.Sends
            .Where(send => send.CampaignId == campaignId
                && send.Status == SendStatuses.Claimed
                && send.SubmittedAt != null
                && send.LeaseExpiresAt != null
                && send.LeaseExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(send => send.Status, SendStatuses.Unknown)
                .SetProperty(send => send.ErrorCode, CampaignsErrorCodes.SendResultUnknown)
                .SetProperty(send => send.LeaseExpiresAt, (DateTime?)null)
                .SetProperty(send => send.UpdatedAt, now), cancellationToken);
        await dbContext.Sends
            .Where(send => send.CampaignId == campaignId
                && send.Status == SendStatuses.Claimed
                && send.SubmittedAt == null
                && send.AttemptCount < _options.MaxAttempts
                && send.LeaseExpiresAt != null
                && send.LeaseExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(send => send.Status, SendStatuses.RetryableFailed)
                .SetProperty(send => send.ErrorCode, (string?)null)
                .SetProperty(send => send.LeaseExpiresAt, (DateTime?)null)
                .SetProperty(send => send.UpdatedAt, now.Subtract(_options.RetryBackoff)), cancellationToken);
        await dbContext.Sends
            .Where(send => send.CampaignId == campaignId
                && send.Status == SendStatuses.Claimed
                && send.SubmittedAt == null
                && send.AttemptCount >= _options.MaxAttempts
                && send.LeaseExpiresAt != null
                && send.LeaseExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(send => send.Status, SendStatuses.Failed)
                .SetProperty(send => send.ErrorCode, CampaignsErrorCodes.SendMailUnavailable)
                .SetProperty(send => send.LeaseExpiresAt, (DateTime?)null)
                .SetProperty(send => send.UpdatedAt, now), cancellationToken);
    }

    /// <summary>
    /// Renews the lease of one claim just before its mail is submitted and stamps the submission. The attempt number is the
    /// claim generation: a claim re-claimed by another worker has a higher attempt count, so the stale holder matches no row
    /// and must not submit. The renewal is also conditional on the claim still being <c>claimed</c> with a lease that has not
    /// ended. Returns false when nothing was renewed.
    /// </summary>
    public async Task<bool> RenewLeaseAsync(ClaimedSend claim, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var renewed = await dbContext.Sends
            .Where(send => send.Id == claim.SendId
                && send.Status == SendStatuses.Claimed
                && send.AttemptCount == claim.AttemptCount
                && send.LeaseExpiresAt != null
                && send.LeaseExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(send => send.LeaseExpiresAt, now.Add(_options.LeaseDuration))
                .SetProperty(send => send.SubmittedAt, now)
                .SetProperty(send => send.UpdatedAt, now), cancellationToken);
        return renewed == 1;
    }

    /// <summary>
    /// Claims up to one batch: new recipients first, then retries that still have attempts left. Returns the claims this
    /// worker holds together with the sendable ones; a suppressed recipient is claimed and skipped, so the claimed count can
    /// be above the sendable count. A zero claimed count means nothing is claimable right now.
    /// </summary>
    public async Task<ClaimedSendBatch> ClaimBatchAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var now = timeProvider.GetUtcNow();
        var leaseEnd = now.Add(_options.LeaseDuration);
        var claimIds = new List<Guid>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var unclaimed = await dbContext.Database
            .SqlQuery<LockedRecipientRow>(CampaignSendClaimSql.LockUnclaimed(organizationId, campaignId, _options.BatchSize))
            .ToListAsync(cancellationToken);
        if (unclaimed.Count > 0)
        {
            var rows = unclaimed
                .Select(row => new SendInsertRow(Guid.CreateVersion7(), row.ContactId, row.RecipientId, CampaignMessageIds.ForRecipient(campaignId, row.ContactId, _options.MessageIdDomain)))
                .ToList();
            await dbContext.Database.ExecuteSqlAsync(
                CampaignSendClaimSql.InsertClaims(organizationId, campaignId, now, leaseEnd, JsonSerializer.Serialize(rows, JsonOptions)),
                cancellationToken);

            var ids = rows.Select(row => row.Id).ToList();
            claimIds.AddRange(await dbContext.Sends
                .AsNoTracking()
                .Where(send => ids.Contains(send.Id))
                .Select(send => send.Id)
                .ToListAsync(cancellationToken));
        }

        var remaining = _options.BatchSize - claimIds.Count;
        if (remaining > 0)
        {
            var retryCutoff = now.Subtract(_options.RetryBackoff);
            var retryIds = await dbContext.Database
                .SqlQuery<Guid>(CampaignSendClaimSql.LockRetries(organizationId, campaignId, _options.MaxAttempts, retryCutoff, remaining))
                .ToListAsync(cancellationToken);
            if (retryIds.Count > 0)
            {
                await dbContext.Sends
                    .Where(send => retryIds.Contains(send.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(send => send.Status, SendStatuses.Claimed)
                        .SetProperty(send => send.AttemptCount, send => send.AttemptCount + 1)
                        .SetProperty(send => send.ClaimedAt, now.UtcDateTime)
                        .SetProperty(send => send.LeaseExpiresAt, leaseEnd.UtcDateTime)
                        .SetProperty(send => send.SubmittedAt, (DateTime?)null)
                        .SetProperty(send => send.ErrorCode, (string?)null)
                        .SetProperty(send => send.UpdatedAt, now.UtcDateTime), cancellationToken);
                claimIds.AddRange(retryIds);
            }
        }

        if (claimIds.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new ClaimedSendBatch([], 0);
        }

        var claims = await dbContext.Sends
            .AsNoTracking()
            .Where(send => claimIds.Contains(send.Id))
            .Select(send => new { send.Id, send.ContactId, send.RecipientId, send.MessageId, send.AttemptCount })
            .ToListAsync(cancellationToken);
        var recipientIds = claims.Select(claim => claim.RecipientId).ToList();
        var recipients = await dbContext.Recipients
            .AsNoTracking()
            .Where(recipient => recipientIds.Contains(recipient.Id))
            .Select(recipient => new { recipient.Id, recipient.EmailSnapshot, recipient.Locale })
            .ToDictionaryAsync(recipient => recipient.Id, cancellationToken);

        var addresses = recipients.Values
            .Select(recipient => SuppressionAddresses.Normalize(recipient.EmailSnapshot))
            .Where(address => address is not null)
            .Select(address => address!)
            .Distinct()
            .ToList();
        var suppressed = addresses.Count == 0
            ? new HashSet<string>(StringComparer.Ordinal)
            : (await dbContext.Suppressions
                .AsNoTracking()
                .Where(suppression => addresses.Contains(suppression.EmailNormalized))
                .Select(suppression => suppression.EmailNormalized)
                .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);

        // A suppression matches the contact or the address: a contact that unsubscribed and then changed its address stays out.
        var claimContactIds = claims.Select(claim => claim.ContactId).Distinct().ToList();
        var suppressedContacts = (await dbContext.Suppressions
            .AsNoTracking()
            .Where(suppression => claimContactIds.Contains(suppression.ContactId))
            .Select(suppression => suppression.ContactId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var suppressedIds = claims
            .Where(claim => suppressedContacts.Contains(claim.ContactId)
                || IsSuppressed(recipients[claim.RecipientId].EmailSnapshot, suppressed))
            .Select(claim => claim.Id)
            .ToList();
        if (suppressedIds.Count > 0)
        {
            await dbContext.Sends
                .Where(send => suppressedIds.Contains(send.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(send => send.Status, SendStatuses.SkippedSuppressed)
                    .SetProperty(send => send.LeaseExpiresAt, (DateTime?)null)
                    .SetProperty(send => send.UpdatedAt, now.UtcDateTime), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var sendable = claims
            .Where(claim => !suppressedIds.Contains(claim.Id))
            .Select(claim =>
            {
                var recipient = recipients[claim.RecipientId];
                return new ClaimedSend(
                    claim.Id,
                    claim.ContactId,
                    claim.RecipientId,
                    claim.MessageId,
                    claim.AttemptCount,
                    recipient.EmailSnapshot ?? string.Empty,
                    recipient.Locale);
            })
            .ToArray();
        return new ClaimedSendBatch(sendable, claimIds.Count);
    }

    private static bool IsSuppressed(string? address, IReadOnlySet<string> suppressed) =>
        SuppressionAddresses.Normalize(address) is { } normalized && suppressed.Contains(normalized);
}
