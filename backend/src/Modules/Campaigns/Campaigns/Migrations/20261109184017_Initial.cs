using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Campaigns.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "campaigns");

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "campaigns",
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
                schema: "campaigns",
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
                name: "outbox_messages",
                schema: "campaigns",
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
                name: "segments",
                schema: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    definition = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_segments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "templates",
                schema: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    subject_liquid = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body_liquid = table.Column<string>(type: "text", nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_expires_at",
                schema: "campaigns",
                table: "idempotency_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "uq_idempotency_keys_organization_key",
                schema: "campaigns",
                table: "idempotency_keys",
                columns: new[] { "organization_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_next_attempt_at",
                schema: "campaigns",
                table: "outbox_messages",
                column: "next_attempt_at",
                filter: "\"processed_at\" IS NULL AND \"dead_lettered_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_segments_organization_id_name_id",
                schema: "campaigns",
                table: "segments",
                columns: new[] { "organization_id", "name", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_templates_organization_id_name_id",
                schema: "campaigns",
                table: "templates",
                columns: new[] { "organization_id", "name", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "campaigns");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "campaigns");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "campaigns");

            migrationBuilder.DropTable(
                name: "segments",
                schema: "campaigns");

            migrationBuilder.DropTable(
                name: "templates",
                schema: "campaigns");
        }
    }
}
