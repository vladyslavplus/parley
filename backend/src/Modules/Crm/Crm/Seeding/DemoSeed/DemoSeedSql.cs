using Parley.Crm.Domain.Activities;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Statements of the demo import. Every statement that touches a business table names the organization first and takes
/// its values as parameters. Table names come from <see cref="CrmSchemaNames"/>, status and type values from the domain constants.
/// Common parameters: @org organization, @source source of the demo rows, @order_source source of the demo orders,
/// @now seed instant, @empty_json default custom fields.
/// </summary>
public static class DemoSeedSql
{
    private static readonly string Contacts = Table(CrmSchemaNames.ContactsTable);

    private static readonly string Companies = Table(CrmSchemaNames.CompaniesTable);

    private static readonly string Orders = Table(CrmSchemaNames.OrdersTable);

    private static readonly string Pipelines = Table(CrmSchemaNames.PipelinesTable);

    private static readonly string PipelineStages = Table(CrmSchemaNames.PipelineStagesTable);

    private static readonly string Deals = Table(CrmSchemaNames.DealsTable);

    private static readonly string DealStageHistory = Table(CrmSchemaNames.DealStageHistoryTable);

    private static readonly string Tasks = Table(CrmSchemaNames.TasksTable);

    private static readonly string Notes = Table(CrmSchemaNames.NotesTable);

    private static readonly string Activities = Table(CrmSchemaNames.ActivitiesTable);

    private static readonly string Tags = Table(CrmSchemaNames.TagsTable);

    private static readonly string ContactsStaging = DemoSeedConstants.ContactsStagingTable;

    private static readonly string OrdersStaging = DemoSeedConstants.OrdersStagingTable;

    public const string CreateContactsStaging =
        "CREATE TEMP TABLE " + DemoSeedConstants.ContactsStagingTable + " ("
        + "id uuid NOT NULL, customer_ref text NOT NULL, first_name text NOT NULL, last_name text NOT NULL, "
        + "email text NOT NULL, phone text NOT NULL, job_title text NOT NULL, country text NULL, city text NULL, "
        + "owner_user_id uuid NOT NULL, company_id uuid NULL, company_name text NULL, "
        + "deal_stage_index integer NULL, deal_title text NULL, deal_lost_reason text NULL, deal_expected_close_on date NULL, "
        + "task_title text NULL, task_due_at timestamptz NULL, task_completed boolean NULL, note_body text NULL) ON COMMIT DROP";

    public const string CreateOrdersStaging =
        "CREATE TEMP TABLE " + DemoSeedConstants.OrdersStagingTable + " ("
        + "id uuid NOT NULL, invoice_no text NOT NULL, customer_ref text NOT NULL, ordered_at timestamptz NOT NULL, "
        + "total_amount numeric(18,2) NOT NULL, line_count integer NOT NULL, is_cancellation boolean NOT NULL) ON COMMIT DROP";

    public const string CopyContacts =
        "COPY " + DemoSeedConstants.ContactsStagingTable
        + " (id, customer_ref, first_name, last_name, email, phone, job_title, country, city, owner_user_id, company_id, company_name, "
        + "deal_stage_index, deal_title, deal_lost_reason, deal_expected_close_on, task_title, task_due_at, task_completed, note_body) "
        + "FROM STDIN (FORMAT BINARY)";

    public const string CopyOrders =
        "COPY " + DemoSeedConstants.OrdersStagingTable
        + " (id, invoice_no, customer_ref, ordered_at, total_amount, line_count, is_cancellation) FROM STDIN (FORMAT BINARY)";

    /// <summary>Counts the default pipeline of the organization; the import creates one only when there is none.</summary>
    public static readonly string CountDefaultPipeline = $"""
        SELECT COUNT(*) FROM {Pipelines} WHERE organization_id = @org AND is_default
        """;

    public static readonly string InsertPipeline = $"""
        INSERT INTO {Pipelines} (id, organization_id, created_at, updated_at, name, system_key, is_default)
        VALUES (@id, @org, @now, @now, @name, @system_key, TRUE)
        """;

    public static readonly string InsertPipelineStage = $"""
        INSERT INTO {PipelineStages}
            (id, organization_id, created_at, updated_at, pipeline_id, name, system_key, position, kind, probability_bps, color_index)
        VALUES (@id, @org, @now, @now, @pipeline_id, @name, @system_key, @position, @kind, @probability_bps, @color_index)
        """;

    /// <summary>Inserts the companies of the staged contacts that do not exist yet.</summary>
    public static readonly string InsertCompanies = $"""
        INSERT INTO {Companies}
            (id, organization_id, created_at, updated_at, name, country, owner_user_id, custom_fields)
        SELECT s.company_id, @org, @now, @now, s.company_name, s.country, s.owner_user_id, CAST(@empty_json AS jsonb)
        FROM {ContactsStaging} s
        WHERE s.company_id IS NOT NULL
        ON CONFLICT (id) DO NOTHING
        """;

    /// <summary>
    /// Inserts the staged customers that do not exist yet. The first order sets the creation instant. The email guard
    /// also absorbs a clash with a manual contact.
    /// </summary>
    public static readonly string InsertContacts = $"""
        INSERT INTO {Contacts}
            (id, organization_id, first_name, last_name, email, email_normalized, phone, job_title, country, city,
             owner_user_id, company_id, source, external_ref, custom_fields, created_at, updated_at)
        SELECT s.id, @org, s.first_name, s.last_name, s.email, lower(s.email), s.phone, s.job_title, s.country, s.city,
               s.owner_user_id, s.company_id, @source, s.customer_ref, CAST(@empty_json AS jsonb),
               COALESCE((SELECT MIN(o.ordered_at) FROM {OrdersStaging} o WHERE o.customer_ref = s.customer_ref), @now), @now
        FROM {ContactsStaging} s
        WHERE NOT EXISTS (
            SELECT 1 FROM {Contacts} c
            WHERE c.organization_id = @org AND c.source = @source AND c.external_ref = s.customer_ref)
        ON CONFLICT DO NOTHING
        """;

    /// <summary>Counts staged invoices whose customer is not in the staged or stored contacts of this source.</summary>
    public static readonly string CountOrdersWithoutContact = $"""
        SELECT COUNT(*)
        FROM {OrdersStaging} s
        WHERE NOT EXISTS (
            SELECT 1 FROM {Contacts} c
            WHERE c.organization_id = @org AND c.source = @source AND c.external_ref = s.customer_ref)
        """;

    /// <summary>Inserts the staged invoices of known customers. The unique key makes a re-run a no-op.</summary>
    public static readonly string InsertOrders = $"""
        INSERT INTO {Orders}
            (id, organization_id, contact_id, external_ref, ordered_at, total_amount, currency, line_count,
             is_cancellation, source, created_at, updated_at)
        SELECT s.id, @org, c.id, s.invoice_no, s.ordered_at, s.total_amount, @currency, s.line_count,
               s.is_cancellation, @order_source, @now, @now
        FROM {OrdersStaging} s
        JOIN {Contacts} c
          ON c.organization_id = @org AND c.source = @source AND c.external_ref = s.customer_ref
        ON CONFLICT (organization_id, source, external_ref) DO NOTHING
        """;

    /// <summary>Sets the latest non-cancellation order on the customers of this import. Only changed rows are written.</summary>
    public static readonly string UpdateLastOrderAt = $"""
        UPDATE {Contacts} c
        SET last_order_at = x.last_order_at
        FROM (
            SELECT o.contact_id, MAX(o.ordered_at) AS last_order_at
            FROM {Orders} o
            WHERE o.organization_id = @org AND o.source = @order_source AND NOT o.is_cancellation
            GROUP BY o.contact_id) x
        WHERE c.organization_id = @org AND c.id = x.contact_id AND c.last_order_at IS DISTINCT FROM x.last_order_at
        """;

    /// <summary>
    /// Creates one open deal for each staged customer with a deal stage who ordered after @recent_since. The amount is the
    /// total of the latest purchase. Won and lost deals close two days after the last order, never after the seed instant.
    /// The deal identifier is derived from the customer, so a re-run skips the deals that exist.
    /// </summary>
    public static readonly string InsertDeals = $"""
        INSERT INTO {Deals}
            (id, organization_id, created_at, updated_at, pipeline_id, stage_id, title, amount, currency, contact_id,
             company_id, owner_user_id, status, lost_reason, expected_close_on, closed_at, board_position, custom_fields, source)
        SELECT md5('demo-deal:' || s.id::text)::uuid, @org, c.last_order_at, @now, p.id, st.id, s.deal_title,
               COALESCE((SELECT o.total_amount FROM {Orders} o
                         WHERE o.organization_id = @org AND o.contact_id = c.id AND o.source = @order_source AND NOT o.is_cancellation
                         ORDER BY o.ordered_at DESC LIMIT 1), 0),
               @currency, c.id, s.company_id, s.owner_user_id,
               CASE WHEN st.kind = '{PipelineStageKinds.Won}' THEN '{DealStatuses.Won}'
                    WHEN st.kind = '{PipelineStageKinds.Lost}' THEN '{DealStatuses.Lost}'
                    ELSE '{DealStatuses.Open}' END,
               CASE WHEN st.kind = '{PipelineStageKinds.Lost}' THEN s.deal_lost_reason END,
               CASE WHEN st.kind = '{PipelineStageKinds.Open}' THEN s.deal_expected_close_on END,
               CASE WHEN st.kind IN ('{PipelineStageKinds.Won}', '{PipelineStageKinds.Lost}')
                    THEN LEAST(@now, c.last_order_at + INTERVAL '2 days') END,
               ROW_NUMBER() OVER (PARTITION BY st.id ORDER BY s.customer_ref) * 1024,
               CAST(@empty_json AS jsonb), @source
        FROM {ContactsStaging} s
        JOIN {Contacts} c
          ON c.organization_id = @org AND c.source = @source AND c.external_ref = s.customer_ref AND c.deleted_at IS NULL
        JOIN {Pipelines} p ON p.organization_id = @org AND p.is_default
        JOIN {PipelineStages} st ON st.pipeline_id = p.id AND st.position = s.deal_stage_index
        WHERE s.deal_stage_index IS NOT NULL AND c.last_order_at >= @recent_since
        ON CONFLICT (id) DO NOTHING
        """;

    /// <summary>History row for the stage a deal was created in (the first stage of the default pipeline).</summary>
    public static readonly string InsertDealHistoryCreated = $"""
        INSERT INTO {DealStageHistory}
            (id, organization_id, created_at, updated_at, deal_id, from_stage_id, to_stage_id, changed_by_user_id, changed_at)
        SELECT md5('demo-deal-history:' || d.id::text || ':created')::uuid, @org, @now, @now, d.id, NULL, first_stage.id,
               d.owner_user_id, d.created_at
        FROM {Deals} d
        JOIN {PipelineStages} first_stage ON first_stage.pipeline_id = d.pipeline_id AND first_stage.position = @first_position
        WHERE d.organization_id = @org AND d.source = @source
        ON CONFLICT (id) DO NOTHING
        """;

    /// <summary>History row for the move from the first stage to the current stage, one day after creation at most.</summary>
    public static readonly string InsertDealHistoryMoved = $"""
        INSERT INTO {DealStageHistory}
            (id, organization_id, created_at, updated_at, deal_id, from_stage_id, to_stage_id, changed_by_user_id, changed_at)
        SELECT md5('demo-deal-history:' || d.id::text || ':moved')::uuid, @org, @now, @now, d.id, first_stage.id, d.stage_id,
               d.owner_user_id, LEAST(@now, d.created_at + INTERVAL '1 day')
        FROM {Deals} d
        JOIN {PipelineStages} first_stage ON first_stage.pipeline_id = d.pipeline_id AND first_stage.position = @first_position
        WHERE d.organization_id = @org AND d.source = @source AND d.stage_id <> first_stage.id
        ON CONFLICT (id) DO NOTHING
        """;

    /// <summary>One task per staged customer that has one, assigned to the customer's owner.</summary>
    public static readonly string InsertTasks = $"""
        INSERT INTO {Tasks}
            (id, organization_id, created_at, updated_at, title, notes, due_at, completed_at, assignee_user_id,
             contact_id, deal_id, company_id, reminder_sent_at, deleted_at)
        SELECT md5('demo-task:' || s.id::text)::uuid, @org, LEAST(@now, s.task_due_at - INTERVAL '3 days'), @now, s.task_title,
               NULL, s.task_due_at, CASE WHEN s.task_completed THEN s.task_due_at END, s.owner_user_id,
               c.id, NULL, NULL, NULL, NULL
        FROM {ContactsStaging} s
        JOIN {Contacts} c
          ON c.organization_id = @org AND c.source = @source AND c.external_ref = s.customer_ref AND c.deleted_at IS NULL
        WHERE s.task_title IS NOT NULL
        ON CONFLICT (id) DO NOTHING
        """;

    /// <summary>One note per staged customer that has one, written at the customer's last order.</summary>
    public static readonly string InsertNotes = $"""
        INSERT INTO {Notes}
            (id, organization_id, created_at, updated_at, body, contact_id, deal_id, company_id, author_user_id, deleted_at)
        SELECT md5('demo-note:' || s.id::text)::uuid, @org, LEAST(@now, COALESCE(c.last_order_at, @now)), @now, s.note_body,
               c.id, NULL, NULL, s.owner_user_id, NULL
        FROM {ContactsStaging} s
        JOIN {Contacts} c
          ON c.organization_id = @org AND c.source = @source AND c.external_ref = s.customer_ref AND c.deleted_at IS NULL
        WHERE s.note_body IS NOT NULL
        ON CONFLICT (id) DO NOTHING
        """;

    /// <summary>
    /// Timeline of the demo workspace in one statement: contact creation, every recorded order, deal stage changes and
    /// closures, notes and completed tasks. Payloads use the field names of the live handlers. Each entry has a derived
    /// source event id, so a re-run inserts nothing. Orders are system events and have no actor; every other entry
    /// carries the owner, author or assignee as its actor.
    /// </summary>
    public static readonly string InsertActivities = $"""
        INSERT INTO {Activities}
            (id, organization_id, created_at, updated_at, type, contact_id, deal_id, company_id, note_id, task_id,
             actor_user_id, payload, occurred_at, source_event_id)
        SELECT e.source_event_id, @org, @now, @now, e.type, e.contact_id, e.deal_id, e.company_id, e.note_id, e.task_id,
               e.actor_user_id, e.payload, e.occurred_at, e.source_event_id
        FROM (
            SELECT '{ActivityTypes.ContactCreated}' AS type, c.id AS contact_id, NULL::uuid AS deal_id, NULL::uuid AS company_id,
                   NULL::uuid AS note_id, NULL::uuid AS task_id, c.owner_user_id AS actor_user_id,
                   CAST('{CrmLimits.EmptyJsonObject}' AS jsonb) AS payload, c.created_at AS occurred_at,
                   md5('demo-activity:contact_created:' || c.id::text)::uuid AS source_event_id
            FROM {Contacts} c
            WHERE c.organization_id = @org AND c.source = @source
            UNION ALL
            SELECT '{ActivityTypes.OrderRecorded}', o.contact_id, NULL::uuid, NULL::uuid, NULL::uuid, NULL::uuid, NULL::uuid,
                   jsonb_build_object('{DemoActivityPayloadKeys.InvoiceNo}', o.external_ref,
                                      '{DemoActivityPayloadKeys.TotalAmount}', o.total_amount,
                                      '{DemoActivityPayloadKeys.Currency}', o.currency,
                                      '{DemoActivityPayloadKeys.IsCancellation}', o.is_cancellation),
                   o.ordered_at, md5('demo-activity:order_recorded:' || o.external_ref)::uuid
            FROM {Orders} o
            WHERE o.organization_id = @org AND o.source = @order_source
            UNION ALL
            SELECT '{ActivityTypes.DealStageChanged}', d.contact_id, d.id, d.company_id, NULL::uuid, NULL::uuid, d.owner_user_id,
                   jsonb_build_object('{DemoActivityPayloadKeys.FromStageId}', h.from_stage_id,
                                      '{DemoActivityPayloadKeys.FromStageName}', fs.name,
                                      '{DemoActivityPayloadKeys.ToStageId}', h.to_stage_id,
                                      '{DemoActivityPayloadKeys.ToStageName}', ts.name,
                                      '{DemoActivityPayloadKeys.Status}', ts.kind),
                   h.changed_at, md5('demo-activity:deal_stage_changed:' || h.id::text)::uuid
            FROM {DealStageHistory} h
            JOIN {Deals} d ON d.id = h.deal_id
            JOIN {PipelineStages} fs ON fs.id = h.from_stage_id
            JOIN {PipelineStages} ts ON ts.id = h.to_stage_id
            WHERE d.organization_id = @org AND d.source = @source AND h.from_stage_id IS NOT NULL
            UNION ALL
            SELECT '{ActivityTypes.DealClosed}', d.contact_id, d.id, d.company_id, NULL::uuid, NULL::uuid, d.owner_user_id,
                   jsonb_build_object('{DemoActivityPayloadKeys.StageName}', st.name, '{DemoActivityPayloadKeys.Status}', st.kind),
                   d.closed_at, md5('demo-activity:deal_closed:' || d.id::text)::uuid
            FROM {Deals} d
            JOIN {PipelineStages} st ON st.id = d.stage_id
            WHERE d.organization_id = @org AND d.source = @source AND d.closed_at IS NOT NULL
            UNION ALL
            SELECT '{ActivityTypes.NoteAdded}', n.contact_id, NULL::uuid, NULL::uuid, n.id, NULL::uuid, n.author_user_id,
                   CAST('{CrmLimits.EmptyJsonObject}' AS jsonb), n.created_at, md5('demo-activity:note_added:' || n.id::text)::uuid
            FROM {Notes} n
            JOIN {Contacts} c ON c.id = n.contact_id
            WHERE n.organization_id = @org AND c.source = @source
            UNION ALL
            SELECT '{ActivityTypes.TaskCompleted}', t.contact_id, NULL::uuid, NULL::uuid, NULL::uuid, t.id, t.assignee_user_id,
                   CAST('{CrmLimits.EmptyJsonObject}' AS jsonb), t.completed_at, md5('demo-activity:task_completed:' || t.id::text)::uuid
            FROM {Tasks} t
            JOIN {Contacts} c ON c.id = t.contact_id
            WHERE t.organization_id = @org AND c.source = @source AND t.completed_at IS NOT NULL
        ) e
        ON CONFLICT DO NOTHING
        """;

    /// <summary>Fixed tags of the demo workspace. The names match the palette index of the tag row.</summary>
    public static IReadOnlyList<string> TagNames { get; } = ["VIP", "Wholesale", "Seasonal", "Newsletter", "Priority"];

    private const string TagShareParameter = "@tag_share";

    private static readonly string TagValues = string.Join(
        ", ",
        TagNames.Select((name, index) => $"('{name.Replace("'", "''", StringComparison.Ordinal)}', {index})"));

    private static readonly string TagCount = TagNames.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Creates the demo tags of the organization; a re-run finds the same names.</summary>
    public static readonly string InsertTags = $"""
        INSERT INTO {Tags} (id, organization_id, created_at, updated_at, name, color_index)
        SELECT md5('demo-tag:' || t.name)::uuid, @org, @now, @now, t.name, t.color_index
        FROM (VALUES {TagValues}) AS t(name, color_index)
        ON CONFLICT (organization_id, name_normalized) DO NOTHING
        """;

    /// <summary>
    /// Links one tag to the share of demo contacts that gets one. The choice is a stable hash of the contact id, so a re-run
    /// links the same pairs.
    /// </summary>
    public static readonly string InsertContactTags = $"""
        INSERT INTO "{CrmSchemaNames.Schema}"."{CrmSchemaNames.ContactTagsTable}"
            (organization_id, contact_id, tag_id)
        SELECT @org, c.id, t.id
        FROM {Contacts} c
        CROSS JOIN (SELECT md5('demo-tag:' || name)::uuid AS id, color_index AS tag_index
                    FROM (VALUES {TagValues}) AS v(name, color_index)) t
        WHERE c.organization_id = @org AND c.source = @source
          AND (abs(hashtext(c.id::text)) % 100) < {TagShareParameter}
          AND t.tag_index = (abs(hashtext(c.id::text)) / 100) % {TagCount}
        ON CONFLICT DO NOTHING
        """;

    private static string Table(string name) => "\"" + CrmSchemaNames.Schema + "\".\"" + name + "\"";
}
