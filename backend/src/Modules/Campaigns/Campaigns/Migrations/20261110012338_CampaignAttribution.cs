using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Campaigns.Migrations
{
    /// <inheritdoc />
    public partial class CampaignAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "campaign_results",
                schema: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    treatment_assigned = table.Column<long>(type: "bigint", nullable: false),
                    holdout_assigned = table.Column<long>(type: "bigint", nullable: false),
                    treatment_targeted = table.Column<long>(type: "bigint", nullable: false),
                    holdout_targeted = table.Column<long>(type: "bigint", nullable: false),
                    treatment_converted = table.Column<long>(type: "bigint", nullable: false),
                    holdout_converted = table.Column<long>(type: "bigint", nullable: false),
                    treatment_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    holdout_revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    rate_treatment = table.Column<double>(type: "double precision", nullable: true),
                    rate_holdout = table.Column<double>(type: "double precision", nullable: true),
                    lift = table.Column<double>(type: "double precision", nullable: true),
                    lift_ci_low = table.Column<double>(type: "double precision", nullable: true),
                    lift_ci_high = table.Column<double>(type: "double precision", nullable: true),
                    p_value = table.Column<double>(type: "double precision", nullable: true),
                    srm_p_value = table.Column<double>(type: "double precision", nullable: true),
                    minimum_detectable_effect = table.Column<double>(type: "double precision", nullable: true),
                    incremental_revenue = table.Column<double>(type: "double precision", nullable: true),
                    simulated = table.Column<bool>(type: "boolean", nullable: false),
                    window_ends_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    window_closed = table.Column<bool>(type: "boolean", nullable: false),
                    computed_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campaign_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipient_outcomes",
                schema: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    converted = table.Column<bool>(type: "boolean", nullable: false),
                    first_conversion_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    orders_in_window = table.Column<int>(type: "integer", nullable: false),
                    simulated_orders_in_window = table.Column<int>(type: "integer", nullable: false),
                    evaluated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipient_outcomes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_campaign_results_campaign_id",
                schema: "campaigns",
                table: "campaign_results",
                column: "campaign_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_recipient_outcomes_campaign_id_recipient_id",
                schema: "campaigns",
                table: "recipient_outcomes",
                columns: new[] { "campaign_id", "recipient_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "campaign_results",
                schema: "campaigns");

            migrationBuilder.DropTable(
                name: "recipient_outcomes",
                schema: "campaigns");
        }
    }
}
