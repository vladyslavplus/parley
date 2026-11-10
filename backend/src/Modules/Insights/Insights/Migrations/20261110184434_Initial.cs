using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Insights.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "insights");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "insights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    request_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    response_status = table.Column<int>(type: "integer", nullable: false),
                    response_body = table.Column<byte[]>(type: "bytea", nullable: true),
                    response_content_type = table.Column<string>(type: "text", nullable: true),
                    response_headers = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "insights",
                columns: table => new
                {
                    consumer = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_messages", x => new { x.consumer, x.event_id });
                });

            migrationBuilder.CreateTable(
                name: "latest_scores",
                schema: "insights",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    score_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    model_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scored_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_latest_scores", x => new { x.organization_id, x.contact_id, x.score_kind });
                });

            migrationBuilder.CreateTable(
                name: "ml_settings",
                schema: "insights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ml_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    active_uplift_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    active_clv_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ml_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "model_versions",
                schema: "insights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    feature_schema_version = table.Column<int>(type: "integer", nullable: false),
                    provenance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    trained_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    metrics = table.Column<string>(type: "jsonb", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_model_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "insights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "priority_items",
                schema: "insights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    priority_value = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    reason_codes = table.Column<string[]>(type: "text[]", nullable: false),
                    suggested_action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    segment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_priority_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rfm_snapshots",
                schema: "insights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    recency_days = table.Column<int>(type: "integer", nullable: true),
                    frequency = table.Column<int>(type: "integer", nullable: false),
                    monetary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    r_tier = table.Column<short>(type: "smallint", nullable: true),
                    f_tier = table.Column<short>(type: "smallint", nullable: true),
                    m_tier = table.Column<short>(type: "smallint", nullable: true),
                    segment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfm_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scores",
                schema: "insights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    scoring_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    model_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    model_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    feature_schema_version = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    scored_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scoring_runs",
                schema: "insights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    model_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    model_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    feature_schema_version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    contacts_total = table.Column<int>(type: "integer", nullable: false),
                    contacts_scored = table.Column<int>(type: "integer", nullable: false),
                    error_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    started_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scoring_runs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_expires_at",
                schema: "insights",
                table: "idempotency_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "uq_idempotency_keys_organization_key",
                schema: "insights",
                table: "idempotency_keys",
                columns: new[] { "organization_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ml_settings_organization_id",
                schema: "insights",
                table: "ml_settings",
                column: "organization_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_model_versions_model_name_version",
                schema: "insights",
                table: "model_versions",
                columns: new[] { "model_name", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_next_attempt_at",
                schema: "insights",
                table: "outbox_messages",
                column: "next_attempt_at",
                filter: "\"processed_at\" IS NULL AND \"dead_lettered_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_priority_items_organization_id_as_of_owner_user_id",
                schema: "insights",
                table: "priority_items",
                columns: new[] { "organization_id", "as_of", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_priority_items_organization_id_as_of_contact_id",
                schema: "insights",
                table: "priority_items",
                columns: new[] { "organization_id", "as_of", "contact_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rfm_snapshots_organization_id_as_of_segment",
                schema: "insights",
                table: "rfm_snapshots",
                columns: new[] { "organization_id", "as_of", "segment" });

            migrationBuilder.CreateIndex(
                name: "ux_rfm_snapshots_organization_id_contact_id_as_of",
                schema: "insights",
                table: "rfm_snapshots",
                columns: new[] { "organization_id", "contact_id", "as_of" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_scores_scoring_run_id_contact_id_score_kind",
                schema: "insights",
                table: "scores",
                columns: new[] { "scoring_run_id", "contact_id", "score_kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "latest_scores",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "ml_settings",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "model_versions",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "priority_items",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "rfm_snapshots",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "scores",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "scoring_runs",
                schema: "insights");
        }
    }
}
