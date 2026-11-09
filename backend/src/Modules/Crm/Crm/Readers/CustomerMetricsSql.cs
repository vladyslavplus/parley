using System.Runtime.CompilerServices;
using Parley.Crm.Persistence;

namespace Parley.Crm.Readers;

/// <summary>
/// The customer metrics statement. Parameters: organization, after contact id, limit, as-of instant, short window
/// start, long window start, open deal status. Every CTE starts from the organization filter.
/// </summary>
public static class CustomerMetricsSql
{
    private const string Contacts = "\"" + CrmSchemaNames.Schema + "\".\"" + CrmSchemaNames.ContactsTable + "\"";

    private const string Orders = "\"" + CrmSchemaNames.Schema + "\".\"" + CrmSchemaNames.OrdersTable + "\"";

    private const string Deals = "\"" + CrmSchemaNames.Schema + "\".\"" + CrmSchemaNames.DealsTable + "\"";

    private const string UtcZone = "'UTC'";

    private const string Statement = $$"""
        WITH page AS (
            SELECT c."id", c."owner_user_id"
            FROM {{Contacts}} c
            WHERE c."organization_id" = {0}
              AND c."deleted_at" IS NULL
              AND ({1}::uuid IS NULL OR c."id" > {1}::uuid)
            ORDER BY c."id"
            LIMIT {2}
        ),
        order_rows AS (
            SELECT o."contact_id", o."ordered_at", o."total_amount", o."is_cancellation"
            FROM {{Orders}} o
            JOIN page p ON p."id" = o."contact_id"
            WHERE o."organization_id" = {0}
              AND o."ordered_at" <= {3}
        ),
        daily AS (
            SELECT r."contact_id",
                   (r."ordered_at" AT TIME ZONE {{UtcZone}})::date AS "day",
                   SUM(r."total_amount") AS "day_total",
                   BOOL_OR(NOT r."is_cancellation") AS "has_purchase"
            FROM order_rows r
            GROUP BY r."contact_id", (r."ordered_at" AT TIME ZONE {{UtcZone}})::date
        ),
        purchase_days AS (
            SELECT d."contact_id", d."day", d."day_total",
                   ROW_NUMBER() OVER (PARTITION BY d."contact_id" ORDER BY d."day") AS "day_rank"
            FROM daily d
            WHERE d."has_purchase"
        ),
        day_agg AS (
            SELECT d."contact_id", COUNT(*) AS "purchase_day_count"
            FROM purchase_days d
            GROUP BY d."contact_id"
        ),
        repeat_agg AS (
            SELECT d."contact_id", AVG(d."day_total") AS "avg_repeat"
            FROM purchase_days d
            WHERE d."day_rank" > 1
            GROUP BY d."contact_id"
        ),
        order_agg AS (
            SELECT r."contact_id",
                   COUNT(*) FILTER (WHERE NOT r."is_cancellation") AS "order_count",
                   MIN(r."ordered_at") FILTER (WHERE NOT r."is_cancellation") AS "first_order_at",
                   MAX(r."ordered_at") FILTER (WHERE NOT r."is_cancellation") AS "last_order_at",
                   COUNT(*) FILTER (WHERE NOT r."is_cancellation" AND r."ordered_at" > {4}) AS "orders_90",
                   COUNT(*) FILTER (WHERE NOT r."is_cancellation" AND r."ordered_at" > {5}) AS "orders_365",
                   SUM(r."total_amount") AS "total_value",
                   SUM(r."total_amount") FILTER (WHERE r."ordered_at" > {5}) AS "value_365"
            FROM order_rows r
            GROUP BY r."contact_id"
        ),
        open_deal AS (
            SELECT DISTINCT dl."contact_id"
            FROM {{Deals}} dl
            JOIN page p ON p."id" = dl."contact_id"
            WHERE dl."organization_id" = {0}
              AND dl."deleted_at" IS NULL
              AND dl."status" = {6}
        )
        SELECT p."id" AS "contact_id",
               p."owner_user_id" AS "owner_user_id",
               oa."first_order_at" AS "first_order_at",
               oa."last_order_at" AS "last_order_at",
               COALESCE(oa."order_count", 0)::int AS "order_count",
               COALESCE(da."purchase_day_count", 0)::int AS "purchase_day_count",
               COALESCE(oa."orders_90", 0)::int AS "orders_short",
               COALESCE(oa."orders_365", 0)::int AS "orders_long",
               COALESCE(oa."total_value", 0) AS "total_value",
               COALESCE(oa."value_365", 0) AS "value_long",
               CASE WHEN COALESCE(oa."order_count", 0) > 0
                    THEN ROUND(COALESCE(oa."total_value", 0) / oa."order_count", 4)
                    ELSE 0 END AS "avg_order_value",
               CASE WHEN ra."avg_repeat" IS NULL THEN 0 ELSE ROUND(ra."avg_repeat", 4) END AS "avg_repeat_order_value",
               (od."contact_id" IS NOT NULL) AS "has_open_deal"
        FROM page p
        LEFT JOIN order_agg oa ON oa."contact_id" = p."id"
        LEFT JOIN day_agg da ON da."contact_id" = p."id"
        LEFT JOIN repeat_agg ra ON ra."contact_id" = p."id"
        LEFT JOIN open_deal od ON od."contact_id" = p."id"
        ORDER BY p."id"
        """;

    public static FormattableString Page(
        Guid organizationId,
        Guid? afterContactId,
        int limit,
        DateTimeOffset asOf,
        DateTimeOffset shortWindowStart,
        DateTimeOffset longWindowStart,
        string openDealStatus) =>
        FormattableStringFactory.Create(Statement, organizationId, afterContactId, limit, asOf, shortWindowStart, longWindowStart, openDealStatus);
}
