using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Campaigns.Migrations
{
    /// <inheritdoc />
    public partial class CampaignDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "send_records",
                schema: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    claimed_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    lease_expires_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    message_id = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_send_records", x => x.id);
                    table.CheckConstraint("ck_send_records_status", "status IN ('claimed', 'sent', 'retryable_failed', 'failed', 'skipped_suppressed', 'unknown')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_send_records_organization_id_campaign_id_status",
                schema: "campaigns",
                table: "send_records",
                columns: new[] { "organization_id", "campaign_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_send_records_campaign_id_contact_id",
                schema: "campaigns",
                table: "send_records",
                columns: new[] { "campaign_id", "contact_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "send_records",
                schema: "campaigns");
        }
    }
}
