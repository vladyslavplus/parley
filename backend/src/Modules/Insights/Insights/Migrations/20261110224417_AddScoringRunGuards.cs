using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Insights.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringRunGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "error_code",
                schema: "insights",
                table: "scoring_runs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "contacts_rejected",
                schema: "insights",
                table: "scoring_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "model_name",
                schema: "insights",
                table: "latest_scores",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "priority_ml_status",
                schema: "insights",
                table: "daily_runs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "priority_mode",
                schema: "insights",
                table: "daily_runs",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_scoring_runs_organization_id_kind_status_finished_at",
                schema: "insights",
                table: "scoring_runs",
                columns: new[] { "organization_id", "kind", "status", "finished_at" });

            migrationBuilder.CreateIndex(
                name: "ux_scoring_runs_organization_id_kind_active",
                schema: "insights",
                table: "scoring_runs",
                columns: new[] { "organization_id", "kind" },
                unique: true,
                filter: "status IN ('queued', 'running')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_scoring_runs_organization_id_kind_status_finished_at",
                schema: "insights",
                table: "scoring_runs");

            migrationBuilder.DropIndex(
                name: "ux_scoring_runs_organization_id_kind_active",
                schema: "insights",
                table: "scoring_runs");

            migrationBuilder.DropColumn(
                name: "contacts_rejected",
                schema: "insights",
                table: "scoring_runs");

            migrationBuilder.DropColumn(
                name: "model_name",
                schema: "insights",
                table: "latest_scores");

            migrationBuilder.DropColumn(
                name: "priority_ml_status",
                schema: "insights",
                table: "daily_runs");

            migrationBuilder.DropColumn(
                name: "priority_mode",
                schema: "insights",
                table: "daily_runs");

            migrationBuilder.AlterColumn<string>(
                name: "error_code",
                schema: "insights",
                table: "scoring_runs",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
