using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Campaigns.Migrations
{
    /// <inheritdoc />
    public partial class CampaignAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campaigns",
                schema: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    holdout_share_bps = table.Column<int>(type: "integer", nullable: false),
                    holdout_seed = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    targeting_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    targeting_share_bps = table.Column<int>(type: "integer", nullable: true),
                    attribution_window_days = table.Column<int>(type: "integer", nullable: false),
                    conversion_rule = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    audience_frozen_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    started_by = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaigns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipients",
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
                    arm = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assignment_bucket = table.Column<int>(type: "integer", nullable: false),
                    score_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    score_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    model_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rank_in_audience = table.Column<int>(type: "integer", nullable: true),
                    is_targeted = table.Column<bool>(type: "boolean", nullable: false),
                    email_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppressions",
                schema: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_normalized = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    suppressed_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppressions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_organization_id_name_id",
                schema: "campaigns",
                table: "campaigns",
                columns: new[] { "organization_id", "name", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_recipients_organization_id_campaign_id_arm_is_targeted",
                schema: "campaigns",
                table: "recipients",
                columns: new[] { "organization_id", "campaign_id", "arm", "is_targeted" });

            migrationBuilder.CreateIndex(
                name: "ux_recipients_campaign_id_contact_id",
                schema: "campaigns",
                table: "recipients",
                columns: new[] { "campaign_id", "contact_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_suppressions_organization_id_email_normalized",
                schema: "campaigns",
                table: "suppressions",
                columns: new[] { "organization_id", "email_normalized" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaigns",
                schema: "campaigns");

            migrationBuilder.DropTable(
                name: "recipients",
                schema: "campaigns");

            migrationBuilder.DropTable(
                name: "suppressions",
                schema: "campaigns");
        }
    }
}
