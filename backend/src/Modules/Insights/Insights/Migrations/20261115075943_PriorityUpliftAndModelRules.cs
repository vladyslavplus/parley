using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Insights.Migrations
{
    /// <inheritdoc />
    public partial class PriorityUpliftAndModelRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "contacts_skipped",
                schema: "insights",
                table: "scoring_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "uplift_segment",
                schema: "insights",
                table: "priority_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "uplift_value",
                schema: "insights",
                table: "priority_items",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "segment_rules",
                schema: "insights",
                table: "model_versions",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contacts_skipped",
                schema: "insights",
                table: "scoring_runs");

            migrationBuilder.DropColumn(
                name: "uplift_segment",
                schema: "insights",
                table: "priority_items");

            migrationBuilder.DropColumn(
                name: "uplift_value",
                schema: "insights",
                table: "priority_items");

            migrationBuilder.DropColumn(
                name: "segment_rules",
                schema: "insights",
                table: "model_versions");
        }
    }
}
