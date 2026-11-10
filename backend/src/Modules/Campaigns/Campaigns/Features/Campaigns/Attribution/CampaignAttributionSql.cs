using System.Runtime.CompilerServices;
using Parley.Campaigns.Persistence;

namespace Parley.Campaigns.Features.Campaigns.Attribution;

/// <summary>
/// Upserts of the attribution job. Both are keyed by the unique constraints, so a re-run updates the same rows and never
/// adds a second one. Every nullable or typed parameter is cast in the statement: an untyped null bound to a numeric column
/// would otherwise be assigned from text, which PostgreSQL does not allow.
/// </summary>
public static class CampaignAttributionSql
{
    private const string Outcomes = "\"" + CampaignsSchemaNames.Schema + "\".\"" + CampaignsSchemaNames.RecipientOutcomesTable + "\"";

    private const string Results = "\"" + CampaignsSchemaNames.Schema + "\".\"" + CampaignsSchemaNames.CampaignResultsTable + "\"";

    private const string UpsertOutcomesStatement = $$"""
        INSERT INTO {{Outcomes}}
            ("id", "organization_id", "created_at", "updated_at", "campaign_id", "recipient_id", "contact_id", "converted",
             "first_conversion_at", "revenue", "orders_in_window", "simulated_orders_in_window", "evaluated_at")
        SELECT o."id", {0}, {1}, {1}, {2}, o."recipient_id", o."contact_id", o."converted", o."first_conversion_at",
               o."revenue", o."orders_in_window", o."simulated_orders_in_window", {1}
        FROM jsonb_to_recordset(CAST({3} AS jsonb)) AS o("id" uuid, "recipient_id" uuid, "contact_id" uuid, "converted" boolean,
             "first_conversion_at" timestamptz, "revenue" numeric, "orders_in_window" integer, "simulated_orders_in_window" integer)
        ON CONFLICT ("campaign_id", "recipient_id") DO UPDATE SET
            "converted" = EXCLUDED."converted",
            "first_conversion_at" = EXCLUDED."first_conversion_at",
            "revenue" = EXCLUDED."revenue",
            "orders_in_window" = EXCLUDED."orders_in_window",
            "simulated_orders_in_window" = EXCLUDED."simulated_orders_in_window",
            "evaluated_at" = EXCLUDED."evaluated_at",
            "updated_at" = EXCLUDED."updated_at"
        """;

    private const string UpsertResultStatement = $$"""
        INSERT INTO {{Results}}
            ("id", "organization_id", "created_at", "updated_at", "campaign_id", "treatment_assigned", "holdout_assigned",
             "treatment_targeted", "holdout_targeted", "treatment_converted", "holdout_converted", "treatment_revenue",
             "holdout_revenue", "rate_treatment", "rate_holdout", "lift", "lift_ci_low", "lift_ci_high", "p_value",
             "srm_p_value", "minimum_detectable_effect", "incremental_revenue", "simulated", "window_ends_at",
             "window_closed", "computed_at")
        VALUES ({0}, {1}, {2}, {2}, {3}, CAST({4} AS bigint), CAST({5} AS bigint), CAST({6} AS bigint), CAST({7} AS bigint),
                CAST({8} AS bigint), CAST({9} AS bigint), CAST({10} AS numeric), CAST({11} AS numeric),
                CAST({12} AS double precision), CAST({13} AS double precision), CAST({14} AS double precision),
                CAST({15} AS double precision), CAST({16} AS double precision), CAST({17} AS double precision),
                CAST({18} AS double precision), CAST({19} AS double precision), CAST({20} AS double precision),
                CAST({21} AS boolean), CAST({22} AS timestamptz), CAST({23} AS boolean), {2})
        ON CONFLICT ("campaign_id") DO UPDATE SET
            "treatment_assigned" = EXCLUDED."treatment_assigned",
            "holdout_assigned" = EXCLUDED."holdout_assigned",
            "treatment_targeted" = EXCLUDED."treatment_targeted",
            "holdout_targeted" = EXCLUDED."holdout_targeted",
            "treatment_converted" = EXCLUDED."treatment_converted",
            "holdout_converted" = EXCLUDED."holdout_converted",
            "treatment_revenue" = EXCLUDED."treatment_revenue",
            "holdout_revenue" = EXCLUDED."holdout_revenue",
            "rate_treatment" = EXCLUDED."rate_treatment",
            "rate_holdout" = EXCLUDED."rate_holdout",
            "lift" = EXCLUDED."lift",
            "lift_ci_low" = EXCLUDED."lift_ci_low",
            "lift_ci_high" = EXCLUDED."lift_ci_high",
            "p_value" = EXCLUDED."p_value",
            "srm_p_value" = EXCLUDED."srm_p_value",
            "minimum_detectable_effect" = EXCLUDED."minimum_detectable_effect",
            "incremental_revenue" = EXCLUDED."incremental_revenue",
            "simulated" = EXCLUDED."simulated",
            "window_ends_at" = EXCLUDED."window_ends_at",
            "window_closed" = EXCLUDED."window_closed",
            "computed_at" = EXCLUDED."computed_at",
            "updated_at" = EXCLUDED."updated_at"
        """;

    /// <summary>Writes the outcome rows of one batch. <paramref name="json"/> holds the rows as a JSON array.</summary>
    public static FormattableString UpsertOutcomes(Guid organizationId, Guid campaignId, DateTimeOffset evaluatedAt, string json) =>
        FormattableStringFactory.Create(UpsertOutcomesStatement, organizationId, evaluatedAt, campaignId, json);

    /// <summary>Writes the campaign's result row. The arguments follow the order of the statement's placeholders.</summary>
    public static FormattableString UpsertResult(params object?[] values) =>
        FormattableStringFactory.Create(UpsertResultStatement, values);
}
