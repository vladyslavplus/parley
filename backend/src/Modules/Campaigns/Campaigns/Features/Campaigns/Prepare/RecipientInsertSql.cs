using System.Runtime.CompilerServices;
using System.Text.Json;
using Parley.Campaigns.Persistence;

namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>
/// Insert statement of one recipient batch. The rows travel as one JSON array, so the statement is one round trip
/// whatever the batch, and nullable columns need no typed arrays. <c>ON CONFLICT DO NOTHING</c> keeps the rows of an
/// earlier run, so a resumed freeze never rewrites a contact. Parameters: organization, campaign, timestamp, JSON rows.
/// </summary>
public static class RecipientInsertSql
{
    private const string Recipients = "\"" + CampaignsSchemaNames.Schema + "\".\"" + CampaignsSchemaNames.RecipientsTable + "\"";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private const string Statement = $$"""
        INSERT INTO {{Recipients}} ("id", "organization_id", "created_at", "updated_at", "campaign_id", "contact_id",
            "arm", "assignment_bucket", "score_kind", "score_value", "model_version", "rank_in_audience", "is_targeted",
            "email_snapshot", "locale")
        SELECT r."id", {0}, {2}, {2}, {1}, r."contact_id", r."arm", r."assignment_bucket", r."score_kind",
               r."score_value", r."model_version", r."rank_in_audience", r."is_targeted", r."email_snapshot", r."locale"
        FROM jsonb_to_recordset({3}::jsonb) AS r("id" uuid, "contact_id" uuid, "arm" text, "assignment_bucket" integer,
            "score_kind" text, "score_value" numeric, "model_version" text, "rank_in_audience" integer,
            "is_targeted" boolean, "email_snapshot" text, "locale" text)
        ON CONFLICT ("campaign_id", "contact_id") DO NOTHING
        """;

    public static string Serialize(IReadOnlyList<RecipientInsertRow> rows) => JsonSerializer.Serialize(rows, JsonOptions);

    public static FormattableString Insert(Guid organizationId, Guid campaignId, DateTimeOffset now, string json) =>
        FormattableStringFactory.Create(Statement, organizationId, campaignId, now, json);
}
