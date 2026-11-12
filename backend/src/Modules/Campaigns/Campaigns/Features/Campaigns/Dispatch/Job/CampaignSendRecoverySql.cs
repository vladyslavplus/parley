using System.Runtime.CompilerServices;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Persistence;

namespace Parley.Campaigns.Features.Campaigns.Dispatch.Job;

/// <summary>
/// The recovery sweep's statement. It is platform-wide, so it carries no organization predicate and reads the tables
/// directly; it selects only the organization and campaign identifiers. A campaign is picked when it is sending, has
/// work, and no runner holds a live claim on it. Work is an unclaimed targeted treatment recipient, a failed claim whose
/// retry is due, or a claim whose lease has ended. A live claim means a runner is active, so the sweep does not start a
/// second runner beside it. A lost enqueue, or a worker that died, leaves no live claim and is picked up.
/// </summary>
public static class CampaignSendRecoverySql
{
    private const string Campaigns = "\"" + CampaignsSchemaNames.Schema + "\".\"" + CampaignsSchemaNames.CampaignsTable + "\"";

    private const string Recipients = "\"" + CampaignsSchemaNames.Schema + "\".\"" + CampaignsSchemaNames.RecipientsTable + "\"";

    private const string Sends = "\"" + CampaignsSchemaNames.Schema + "\".\"" + CampaignsSchemaNames.SendRecordsTable + "\"";

    private const string CandidatesStatement = $$"""
        SELECT c."organization_id" AS "organization_id", c."id" AS "campaign_id"
        FROM {{Campaigns}} c
        WHERE c."status" = {0}
          AND NOT EXISTS (
              SELECT 1 FROM {{Sends}} live
              WHERE live."campaign_id" = c."id" AND live."status" = {1} AND live."lease_expires_at" > {2})
          AND (
              EXISTS (
                  SELECT 1 FROM {{Recipients}} r
                  WHERE r."campaign_id" = c."id" AND r."arm" = {3} AND r."is_targeted"
                    AND NOT EXISTS (
                        SELECT 1 FROM {{Sends}} s
                        WHERE s."campaign_id" = c."id" AND s."contact_id" = r."contact_id"))
              OR EXISTS (
                  SELECT 1 FROM {{Sends}} s
                  WHERE s."campaign_id" = c."id" AND s."status" = {4} AND s."attempt_count" < {5} AND s."updated_at" <= {6})
              OR EXISTS (
                  SELECT 1 FROM {{Sends}} s
                  WHERE s."campaign_id" = c."id" AND s."status" = {1} AND s."lease_expires_at" <= {2}))
        """;

    /// <param name="now">The sweep time. Compared with the lease ends of the claims.</param>
    /// <param name="retryCutoff">A failed claim last written before this is due for retry.</param>
    /// <param name="maxAttempts">Attempts a failed claim may still use.</param>
    public static FormattableString Candidates(DateTimeOffset now, DateTimeOffset retryCutoff, int maxAttempts) =>
        FormattableStringFactory.Create(
            CandidatesStatement,
            CampaignStatuses.Sending,
            SendStatuses.Claimed,
            now,
            HoldoutArms.Treatment,
            SendStatuses.RetryableFailed,
            maxAttempts,
            retryCutoff);
}
