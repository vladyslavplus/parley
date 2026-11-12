using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Parley.Notifications.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telegram_deliveries",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    claimed_until = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    error_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_deliveries", x => x.id);
                    table.CheckConstraint("ck_telegram_deliveries_status", "Status IN ('pending', 'sent', 'blocked', 'failed', 'cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "telegram_link_codes",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_link_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telegram_links",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    linked_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    deactivated_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telegram_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telegram_updates",
                schema: "notifications",
                columns: table => new
                {
                    update_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    received_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_updates", x => x.update_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_telegram_deliveries_status_next_attempt_at",
                schema: "notifications",
                table: "telegram_deliveries",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ux_telegram_deliveries_notification_id_chat_id",
                schema: "notifications",
                table: "telegram_deliveries",
                columns: new[] { "notification_id", "chat_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_telegram_link_codes_code_hash",
                schema: "notifications",
                table: "telegram_link_codes",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_telegram_links_organization_id_chat_id_active",
                schema: "notifications",
                table: "telegram_links",
                columns: new[] { "organization_id", "chat_id" },
                unique: true,
                filter: "active");

            migrationBuilder.CreateIndex(
                name: "ux_telegram_links_organization_id_user_id_active",
                schema: "notifications",
                table: "telegram_links",
                columns: new[] { "organization_id", "user_id" },
                unique: true,
                filter: "active");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_updates_received_at",
                schema: "notifications",
                table: "telegram_updates",
                column: "received_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_deliveries",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "telegram_link_codes",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "telegram_links",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "telegram_updates",
                schema: "notifications");
        }
    }
}
