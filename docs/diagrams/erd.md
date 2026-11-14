# Entity relationship diagrams

Source: the live schema of the verification stack (`information_schema` on the database built by the EF Core migrations, 2026-11-13), not a hand-written model. Each module owns one PostgreSQL schema and its own `DbContext`; cross-schema references are logical identifiers and have no foreign-key constraint. Only the constraints that exist in the database are drawn as relationships; the reading guide says which links are logical.

## Overview

```mermaid
flowchart LR
    identity[("identity<br/>organizations, users, teams,<br/>roles, memberships, invitations,<br/>refresh tokens, audit")]
    crm[("crm<br/>contacts, companies, pipelines,<br/>deals, tasks, notes, activities,<br/>orders, tags, custom fields")]
    campaigns[("campaigns<br/>templates, segments, campaigns,<br/>recipients, send records,<br/>suppressions, results")]
    insights[("insights<br/>daily runs, RFM snapshots,<br/>priority items, ML settings,<br/>models, scoring runs, scores")]
    notifications[("notifications<br/>in-app notifications,<br/>preferences, Telegram links<br/>and deliveries")]

    identity -->|"organization_id and user ids<br/>on every business row"| crm
    crm -->|"contact ids, orders<br/>and timeline activities"| campaigns
    insights -->|"scores and priority facts"| crm
    insights -->|"score ids, priority items"| campaigns
    notifications -->|"user ids, event ids"| identity
    campaigns -->|"sent, unsubscribed<br/>and completed events"| notifications
```

Reading guide: five schemas, one per module, with no shared tables. Every tenant-scoped business table carries `organization_id` and is filtered by the tenant query filter; that column is not repeated in the per-schema diagrams. Global tables (`users`, `roles` for system roles, `model_versions`, `telegram_updates`, and `demo_seed_states`, which is keyed by organization) are the exceptions. The overview shows only the links that matter across module boundaries: Identity supplies organization and user identifiers to every module; Crm supplies contacts, orders and timeline activities to Campaigns; Insights reads CRM facts and publishes scores that Campaigns uses when a campaign targets by score; Notifications resolves user ids and consumes events. These arrows are logical, not foreign keys. The per-schema diagrams below distinguish real constraints from logical links.

## identity

```mermaid
erDiagram
    ORGANIZATIONS {
        uuid id PK
        string name
        string slug
        string time_zone
        string default_locale
        char currency
        string status
    }
    USERS {
        uuid id PK
        string email
        string display_name
        string locale
        string theme
        uuid last_active_organization_id FK
        boolean is_platform_operator
        boolean email_confirmed
    }
    TEAMS {
        uuid id PK
        uuid organization_id
        string name
    }
    ROLES {
        uuid id PK
        uuid organization_id
        string code
        boolean is_system
    }
    ROLE_PERMISSIONS {
        uuid role_id FK
        string permission
        string scope
    }
    MEMBERSHIPS {
        uuid id PK
        uuid organization_id
        uuid user_id FK
        uuid role_id FK
        uuid team_id FK
        string status
        timestamp joined_at
    }
    INVITATIONS {
        uuid id PK
        uuid organization_id
        string email_normalized
        uuid role_id FK
        uuid team_id FK
        bytea token_hash
        timestamp expires_at
        timestamp accepted_at
        timestamp revoked_at
        uuid invited_by
    }
    REFRESH_TOKENS {
        uuid id PK
        uuid user_id FK
        uuid family_id
        bytea token_hash
        uuid organization_id
        timestamp expires_at
        timestamp revoked_at
        uuid replaced_by_id
    }
    AUDIT_EVENTS {
        uuid id PK
        uuid organization_id
        uuid actor_user_id
        string action
        string target_type
        uuid target_id
        jsonb metadata
        timestamp occurred_at
        uuid source_event_id
    }
    DEMO_SEED_STATES {
        uuid organization_id PK
        timestamp seeded_at
    }

    ORGANIZATIONS ||--o{ MEMBERSHIPS : "has"
    ORGANIZATIONS ||--o{ TEAMS : "has"
    ORGANIZATIONS ||--o{ ROLES : "owns custom roles"
    ORGANIZATIONS ||--o{ USERS : "is the last active organization"
    ORGANIZATIONS ||--o{ INVITATIONS : "has"
    USERS ||--o{ MEMBERSHIPS : "joins through"
    USERS ||--o{ REFRESH_TOKENS : "owns sessions"
    USERS ||--o{ AUDIT_EVENTS : "acts in"
    ROLES ||--o{ MEMBERSHIPS : "grants"
    ROLES ||--o{ ROLE_PERMISSIONS : "has grants"
    ROLES ||--o{ INVITATIONS : "reserves"
    TEAMS ||--o{ MEMBERSHIPS : "contains"
    TEAMS ||--o{ INVITATIONS : "reserves"
```

Reading guide: `organizations` has no `organization_id` of its own, because it is the tenant. A user is global and joins organizations through `memberships`; one member has exactly one role per organization, and a role is either one of the five system roles (`organization_id` null) or the reserved shape of a custom role. `role_permissions` is a join table whose scope text (`own`, `team`, `org`, or null for an organization-level capability) is part of the grant, not of the permission string. Refresh tokens form families: `family_id` ties a rotated chain, `replaced_by_id` marks rotation, and a terminal revocation has no successor; only a SHA-256 hash of the token value is stored. `audit_events` is written from the audit outbox and carries `source_event_id` for deduplication. The three ASP.NET Identity support tables (`user_claims`, `user_logins`, `user_tokens`) and the per-schema `outbox_messages`, `inbox_messages` and `idempotency_keys` are plumbing and are omitted here. The only physical foreign keys are the ones drawn; `audit_events.actor_user_id`, `invitations.invited_by` and `roles.organization_id` are logical identifiers.

## crm

```mermaid
erDiagram
    CONTACTS {
        uuid id PK
        string first_name
        string last_name
        string email
        string phone
        uuid company_id FK
        uuid owner_user_id
        jsonb custom_fields
        timestamp deleted_at
        timestamp last_order_at
    }
    COMPANIES {
        uuid id PK
        string name
        string domain
        uuid owner_user_id
        jsonb custom_fields
        timestamp deleted_at
    }
    PIPELINES {
        uuid id PK
        string name
        boolean is_default
        string system_key
    }
    PIPELINE_STAGES {
        uuid id PK
        uuid pipeline_id FK
        string name
        int position
        string kind
        int probability_bps
    }
    DEALS {
        uuid id PK
        uuid pipeline_id FK
        uuid stage_id FK
        string title
        numeric amount
        uuid contact_id
        uuid company_id
        uuid owner_user_id
        string status
        bigint board_position
        timestamp deleted_at
    }
    DEAL_STAGE_HISTORY {
        uuid id PK
        uuid deal_id
        uuid from_stage_id
        uuid to_stage_id
        uuid changed_by_user_id
        timestamp changed_at
    }
    TASKS {
        uuid id PK
        string title
        timestamp due_at
        timestamp completed_at
        uuid assignee_user_id
        uuid contact_id
        uuid deal_id
        uuid company_id
        timestamp deleted_at
    }
    NOTES {
        uuid id PK
        string body
        uuid contact_id
        uuid deal_id
        uuid company_id
        uuid author_user_id
        timestamp deleted_at
    }
    ACTIVITIES {
        uuid id PK
        string type
        uuid contact_id
        uuid company_id
        uuid deal_id
        uuid note_id
        uuid task_id
        uuid actor_user_id
        jsonb payload
        timestamp occurred_at
        uuid source_event_id
    }
    ORDERS {
        uuid id PK
        uuid contact_id FK
        string external_ref
        timestamp ordered_at
        numeric total_amount
        char currency
        int line_count
        boolean is_cancellation
        string source
    }
    TAGS {
        uuid id PK
        string name
        smallint color_index
    }
    CONTACT_TAGS {
        uuid organization_id
        uuid contact_id FK
        uuid tag_id FK
    }
    CUSTOM_FIELD_DEFINITIONS {
        uuid id PK
        string entity_type
        string key
        string label
        string field_type
        jsonb options
        boolean is_required
        int position
        timestamp archived_at
    }
    CONTACT_EXPORTS {
        uuid id PK
        uuid requested_by_user_id
        string status
        jsonb filter_json
        string file_name
        int row_count
        timestamp completed_at
        timestamp expires_at
    }

    COMPANIES ||--o{ CONTACTS : "employs"
    PIPELINES ||--o{ PIPELINE_STAGES : "orders"
    PIPELINES ||--o{ DEALS : "contains"
    PIPELINE_STAGES ||--o{ DEALS : "holds"
    DEALS ||--o{ DEAL_STAGE_HISTORY : "moves through"
    CONTACTS ||--o{ ORDERS : "places"
    CONTACTS ||--o{ CONTACT_TAGS : "is tagged"
    TAGS ||--o{ CONTACT_TAGS : "tags"
    CONTACTS ||--o{ DEALS : "is about"
    COMPANIES ||--o{ DEALS : "is about"
    CONTACTS ||--o{ TASKS : "is about"
    DEALS ||--o{ TASKS : "is about"
    CONTACTS ||--o{ NOTES : "is about"
    DEALS ||--o{ NOTES : "is about"
    CONTACTS ||--o{ ACTIVITIES : "has timeline"
```

Reading guide: the CRM schema is the largest and the only one where most relationships are logical rather than physical. The physical foreign keys are `contacts.company_id`, `deals.pipeline_id`, `deals.stage_id`, `pipeline_stages.pipeline_id`, `orders.contact_id`, and both columns of `contact_tags`. The arrows from contacts and companies to deals, tasks, notes and activities, and from deals to stage history, are application-level identifiers: the records cross module-internal aggregates and are validated in handlers, not by the database. `activities` is the timeline row for contact, company, deal, task and note events; `source_event_id` deduplicates events that arrive twice. `orders` is the imported purchase fact used by RFM, CLV and scoring, and `external_ref` makes the import idempotent per organization. Soft delete is `deleted_at` on contacts, companies, deals, tasks and notes; lists and search exclude those rows. `custom_fields` is JSONB on contacts, companies and deals, validated against `custom_field_definitions`.

## campaigns

```mermaid
erDiagram
    TEMPLATES {
        uuid id PK
        string name
        string subject_liquid
        text body_liquid
        string locale
        timestamp archived_at
    }
    SEGMENTS {
        uuid id PK
        string name
        jsonb definition
        timestamp archived_at
    }
    CAMPAIGNS {
        uuid id PK
        string name
        uuid template_id
        uuid segment_id
        string status
        int holdout_share_bps
        string holdout_seed
        string targeting_mode
        int targeting_share_bps
        int attribution_window_days
        string conversion_rule
        timestamp prepare_as_of
        timestamp audience_frozen_at
        timestamp started_at
        timestamp completed_at
        uuid started_by
        string source
        timestamp scheduled_at
    }
    RECIPIENTS {
        uuid id PK
        uuid campaign_id
        uuid contact_id
        string arm
        int assignment_bucket
        string score_kind
        numeric score_value
        string model_version
        int rank_in_audience
        boolean is_targeted
        string email_snapshot
        string locale
    }
    SEND_RECORDS {
        uuid id PK
        uuid campaign_id
        uuid contact_id
        uuid recipient_id
        string status
        int attempt_count
        timestamp claimed_at
        timestamp lease_expires_at
        timestamp submitted_at
        timestamp sent_at
        string message_id
        string error_code
    }
    SUPPRESSIONS {
        uuid id PK
        uuid contact_id
        string email_normalized
        string reason
        timestamp suppressed_at
    }
    CAMPAIGN_RESULTS {
        uuid id PK
        uuid campaign_id
        bigint treatment_assigned
        bigint holdout_assigned
        bigint treatment_targeted
        bigint holdout_targeted
        bigint treatment_converted
        bigint holdout_converted
        numeric treatment_revenue
        numeric holdout_revenue
        double lift
        double lift_ci_low
        double lift_ci_high
        double p_value
        double srm_p_value
        double minimum_detectable_effect
        double incremental_revenue
        boolean simulated
        timestamp window_ends_at
        boolean window_closed
        timestamp computed_at
    }
    RECIPIENT_OUTCOMES {
        uuid id PK
        uuid campaign_id
        uuid recipient_id
        uuid contact_id
        boolean converted
        timestamp first_conversion_at
        numeric revenue
        int orders_in_window
        int simulated_orders_in_window
        timestamp evaluated_at
    }

    TEMPLATES ||--o{ CAMPAIGNS : "renders"
    SEGMENTS ||--o{ CAMPAIGNS : "filters"
    CAMPAIGNS ||--o{ RECIPIENTS : "freezes"
    CAMPAIGNS ||--o{ SEND_RECORDS : "claims"
    RECIPIENTS ||--o| SEND_RECORDS : "has at most one claim"
    CAMPAIGNS ||--o| CAMPAIGN_RESULTS : "stores one result"
    RECIPIENTS ||--o| RECIPIENT_OUTCOMES : "evaluates"
```

Reading guide: `recipients` is the frozen audience and is written once; `arm` (`treatment`/`holdout`), `assignment_bucket` (0-9999), `score_*`, `rank_in_audience`, `is_targeted` and `email_snapshot` are the values at the freeze instant. `send_records` is the idempotency guard: one claim per campaign and contact, a lease with `submitted_at` stamped right before submission, and a status from the send state machine. The unique claim is what makes a retry or a restart safe. `suppressions` matches either the normalized address or the contact id and is checked at claim time, after the freeze, so an unsubscribe between the freeze and the send is still honoured; it is not drawn as a relationship because the claim matches it by value, not by a stored identifier. `campaign_results` holds one row per campaign with both the assignment counts (for the sample ratio check) and the targeted counts (for the lift comparison); `simulated` distinguishes the demo's injected effect from the A/A fixture. `recipient_outcomes` records what happened in the attribution window and is rebuilt by the attribution run. This schema has no physical foreign keys: each aggregate is written by one transaction, and cross-aggregate reads are scoped by campaign id, so every relationship drawn is a logical identifier.

## insights

```mermaid
erDiagram
    DAILY_RUNS {
        uuid id PK
        date as_of
        timestamp rfm_completed_at
        timestamp priority_completed_at
        text priority_ml_status
        text priority_mode
    }
    RFM_SNAPSHOTS {
        uuid id PK
        uuid contact_id
        uuid owner_user_id
        date as_of
        int recency_days
        int frequency
        numeric monetary
        smallint r_tier
        smallint f_tier
        smallint m_tier
        string segment
    }
    PRIORITY_ITEMS {
        uuid id PK
        uuid contact_id
        uuid owner_user_id
        date as_of
        int rank
        string mode
        numeric priority_value
        string_array reason_codes
        string suggested_action
        string segment
    }
    ML_SETTINGS {
        uuid id PK
        boolean ml_enabled
        string active_uplift_version
        string active_clv_version
        uuid updated_by
    }
    MODEL_VERSIONS {
        uuid id PK
        string model_name
        string version
        string kind
        int feature_schema_version
        string provenance
        timestamp trained_at
        jsonb metrics
        timestamp synced_at
        boolean is_available
    }
    SCORING_RUNS {
        uuid id PK
        string kind
        string model_name
        string model_version
        int feature_schema_version
        string status
        int contacts_total
        int contacts_scored
        int contacts_rejected
        string error_code
        timestamp started_at
        timestamp finished_at
        uuid started_by
    }
    SCORES {
        uuid id PK
        uuid scoring_run_id
        uuid contact_id
        string score_kind
        string model_name
        string model_version
        int feature_schema_version
        numeric value
        jsonb payload
        timestamp scored_at
    }
    LATEST_SCORES {
        uuid organization_id PK
        uuid contact_id PK
        string score_kind PK
        uuid score_id
        string model_name
        string model_version
        numeric value
        jsonb payload
        timestamp scored_at
    }

    SCORING_RUNS ||--o{ SCORES : "produces"
    SCORING_RUNS ||--o{ LATEST_SCORES : "swaps in"
    MODEL_VERSIONS ||--o{ SCORING_RUNS : "pins"
    DAILY_RUNS ||--o{ PRIORITY_ITEMS : "ranks"
    DAILY_RUNS ||--o{ RFM_SNAPSHOTS : "computes"
    RFM_SNAPSHOTS }o--|| PRIORITY_ITEMS : "feeds the RFM mode"
    LATEST_SCORES }o--|| PRIORITY_ITEMS : "feeds the ML mode"
```

Reading guide: one `daily_runs` row per organization and CRM day is the completion marker for the RFM snapshot and the priority build, and it also stores the mode the list was built in. `rfm_snapshots` is one row per contact per day; `priority_items` is the ranked list with the reason codes, segment and suggested action that the UI shows. In RFM mode the priority value comes from the RFM weights; in ML mode it is the additive uplift-based value, and the `mode` column records which rule produced the row. Scoring is isolated in `scoring_runs`, `scores` and `latest_scores`: a run writes its own rows, and only a successful run swaps them into `latest_scores` in one transaction, so a failed run never changes what the product reads. `model_versions` is the synced registry with provenance and metrics; a run pins the version it used, and the freeze pins the same version onto the campaign. All links here are logical; there are no physical foreign keys.

## notifications

```mermaid
erDiagram
    NOTIFICATIONS {
        uuid id PK
        uuid user_id
        text type
        jsonb parameters
        text link_route
        timestamp read_at
        uuid source_event_id
    }
    PREFERENCES {
        uuid id PK
        uuid user_id
        text type
        text channel
        boolean enabled
    }
    TELEGRAM_LINKS {
        uuid id PK
        uuid user_id
        bigint chat_id
        string locale
        boolean active
        timestamp linked_at
        timestamp deactivated_at
    }
    TELEGRAM_LINK_CODES {
        uuid id PK
        uuid user_id
        bytea code_hash
        timestamp expires_at
        timestamp used_at
    }
    TELEGRAM_DELIVERIES {
        uuid id PK
        uuid notification_id
        uuid user_id
        bigint chat_id
        text status
        int attempts
        timestamp next_attempt_at
        timestamp claimed_until
        timestamp sent_at
        string error_code
    }
    TELEGRAM_UPDATES {
        bigint update_id PK
        timestamp received_at
    }

    NOTIFICATIONS ||--o{ TELEGRAM_DELIVERIES : "delivers through"
    TELEGRAM_LINKS ||--o{ TELEGRAM_DELIVERIES : "targets a chat"
    TELEGRAM_LINK_CODES }o--|| TELEGRAM_LINKS : "redeems into"
```

Reading guide: an in-app notification is written by a consumer inside its inbox transaction, so a replayed event produces no duplicate; `source_event_id` is the deduplication key. `preferences` is one row per user, notification type and channel, and it is checked before an in-app row or a Telegram delivery is planned. Telegram is a separate delivery pipeline: a link code is hashed and single-use, a link binds one chat, and `telegram_deliveries` is a leased outbox with attempts, backoff (`next_attempt_at`) and a stable error code. `telegram_updates` stores the Telegram `update_id` so a replayed webhook or polled update is a no-op. All identifiers are logical; there are no physical foreign keys in this schema.

## Plumbing tables

Every module schema also contains `outbox_messages`, `inbox_messages` and (except Notifications and Identity) `idempotency_keys`. They are infrastructure, not business entities: the outbox is the transactional event queue, the inbox is the per-consumer deduplication record, and idempotency keys guard retried mutating requests. Their columns and lifecycle are drawn in the outbox sequence diagram, not here.
