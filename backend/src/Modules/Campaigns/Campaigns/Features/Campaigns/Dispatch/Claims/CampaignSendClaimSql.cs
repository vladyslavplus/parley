using System.Runtime.CompilerServices;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Persistence;

namespace Parley.Campaigns.Features.Campaigns.Dispatch.Claims;

/// <summary>
/// Locking statements of the claim step. The row locks are taken first, in their own statement, and the claim insert
/// runs afterwards with a fresh snapshot, so a contact that another worker claimed in the meantime is never claimed
/// twice. Every statement carries the organization as its first predicate.
/// </summary>
public static class CampaignSendClaimSql
{
    private const string Recipients = "\"" + CampaignsSchemaNames.Schema + "\".\"" + CampaignsSchemaNames.RecipientsTable + "\"";

    private const string Sends = "\"" + CampaignsSchemaNames.Schema + "\".\"" + CampaignsSchemaNames.SendRecordsTable + "\"";

    private const string LockUnclaimedStatement = $$"""
        SELECT r."id" AS "recipient_id", r."contact_id" AS "contact_id"
        FROM {{Recipients}} r
        WHERE r."organization_id" = {0}
          AND r."campaign_id" = {1}
          AND r."arm" = {2}
          AND r."is_targeted"
          AND NOT EXISTS (
              SELECT 1 FROM {{Sends}} s
              WHERE s."organization_id" = {0} AND s."campaign_id" = {1} AND s."contact_id" = r."contact_id")
        ORDER BY r."id"
        LIMIT {3}
        FOR UPDATE OF r SKIP LOCKED
        """;

    /// <summary>
    /// A failed claim is due for retry once its last write (<c>updated_at</c>, set by the failure) is older than the backoff.
    /// </summary>
    private const string LockRetryStatement = $$"""
        SELECT s."id" AS "id"
        FROM {{Sends}} s
        WHERE s."organization_id" = {0}
          AND s."campaign_id" = {1}
          AND s."status" = {2}
          AND s."attempt_count" < {3}
          AND s."updated_at" <= {4}
        ORDER BY s."id"
        LIMIT {5}
        FOR UPDATE SKIP LOCKED
        """;

    private const string InsertClaimsStatement = $$"""
        INSERT INTO {{Sends}} ("id", "organization_id", "created_at", "updated_at", "campaign_id", "contact_id",
            "recipient_id", "status", "attempt_count", "claimed_at", "lease_expires_at", "sent_at", "message_id", "error_code")
        SELECT c."id", {0}, {1}, {1}, {2}, c."contact_id", c."recipient_id", {3}, 1, {1}, {4}, NULL, c."message_id", NULL
        FROM jsonb_to_recordset({5}::jsonb) AS c("id" uuid, "contact_id" uuid, "recipient_id" uuid, "message_id" text)
        WHERE NOT EXISTS (
            SELECT 1 FROM {{Sends}} s
            WHERE s."organization_id" = {0} AND s."campaign_id" = {2} AND s."contact_id" = c."contact_id")
        ON CONFLICT ("campaign_id", "contact_id") DO NOTHING
        """;

    /// <summary>Locks up to <paramref name="limit"/> targeted treatment recipients that have no claim yet.</summary>
    public static FormattableString LockUnclaimed(Guid organizationId, Guid campaignId, int limit) =>
        FormattableStringFactory.Create(LockUnclaimedStatement, organizationId, campaignId, HoldoutArms.Treatment, limit);

    /// <summary>Locks up to <paramref name="limit"/> failed claims that still have attempts left and whose retry is due at <paramref name="retryCutoff"/>.</summary>
    public static FormattableString LockRetries(Guid organizationId, Guid campaignId, int maxAttempts, DateTimeOffset retryCutoff, int limit) =>
        FormattableStringFactory.Create(
            LockRetryStatement,
            organizationId,
            campaignId,
            SendStatuses.RetryableFailed,
            maxAttempts,
            retryCutoff,
            limit);

    /// <summary>
    /// Inserts claims for recipients with no claim yet. The rows travel as one JSON array; a row that conflicts with a
    /// claim written in the meantime is skipped by the unique key.
    /// </summary>
    public static FormattableString InsertClaims(
        Guid organizationId,
        Guid campaignId,
        DateTimeOffset now,
        DateTimeOffset leaseEnd,
        string json) =>
        FormattableStringFactory.Create(
            InsertClaimsStatement,
            organizationId,
            now,
            campaignId,
            SendStatuses.Claimed,
            leaseEnd,
            json);
}
