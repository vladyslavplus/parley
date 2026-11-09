using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class ContactExportsAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contact_exports",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    filter_json = table.Column<string>(type: "jsonb", nullable: false),
                    file_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    row_count = table.Column<int>(type: "integer", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_exports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "crm",
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

            migrationBuilder.CreateIndex(
                name: "ix_contact_exports_expires_at",
                schema: "crm",
                table: "contact_exports",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_contact_exports_organization_id_requested_by_created",
                schema: "crm",
                table: "contact_exports",
                columns: new[] { "organization_id", "requested_by_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_expires_at",
                schema: "crm",
                table: "idempotency_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "uq_idempotency_keys_organization_key",
                schema: "crm",
                table: "idempotency_keys",
                columns: new[] { "organization_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_exports",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "crm");
        }
    }
}
