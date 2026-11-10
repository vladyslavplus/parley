using System.Runtime.CompilerServices;
using System.Text.Json;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Rfm.Compute;

/// <summary>
/// Upsert of one snapshot batch. The rows travel as one JSON array, so the statement is one round trip whatever the batch.
/// The unique key (organization, contact, day) makes a repeated run rewrite the same rows, and <c>updated_at</c> marks each
/// row this run touched, so the run can delete the rows of the day it did not touch.
/// Parameters: organization, stamp (the run instant), organization day, JSON rows.
/// </summary>
public static class RfmSnapshotSql
{
    private const string Table = "\"" + InsightsSchemaNames.Schema + "\".\"" + InsightsSchemaNames.RfmSnapshotsTable + "\"";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private const string Statement = $$"""
        INSERT INTO {{Table}} ("id", "organization_id", "created_at", "updated_at", "contact_id", "owner_user_id", "as_of",
            "recency_days", "frequency", "monetary", "r_tier", "f_tier", "m_tier", "segment")
        SELECT r."id", {0}, {1}, {1}, r."contact_id", r."owner_user_id", {2}, r."recency_days", r."frequency",
            r."monetary", r."r_tier", r."f_tier", r."m_tier", r."segment"
        FROM jsonb_to_recordset({3}::jsonb) AS r("id" uuid, "contact_id" uuid, "owner_user_id" uuid, "recency_days" integer,
            "frequency" integer, "monetary" numeric, "r_tier" smallint, "f_tier" smallint, "m_tier" smallint, "segment" text)
        ON CONFLICT ("organization_id", "contact_id", "as_of") DO UPDATE SET
            "owner_user_id" = EXCLUDED."owner_user_id",
            "recency_days" = EXCLUDED."recency_days",
            "frequency" = EXCLUDED."frequency",
            "monetary" = EXCLUDED."monetary",
            "r_tier" = EXCLUDED."r_tier",
            "f_tier" = EXCLUDED."f_tier",
            "m_tier" = EXCLUDED."m_tier",
            "segment" = EXCLUDED."segment",
            "updated_at" = EXCLUDED."updated_at"
        """;

    public static string Serialize(IReadOnlyList<RfmSnapshotRow> rows) => JsonSerializer.Serialize(rows, JsonOptions);

    public static FormattableString Upsert(Guid organizationId, DateTimeOffset stamp, DateOnly asOf, string json) =>
        FormattableStringFactory.Create(Statement, organizationId, stamp, asOf, json);
}
