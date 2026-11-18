using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class PipelineStageColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                schema: "crm",
                table: "pipeline_stages",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            // Data step: a stage that named a step of the five-step blue ramp takes that step as a hex colour.
            // A row outside the five steps (no such row exists, the old check constraint allowed only 0 to 4)
            // would fall back to the lightest step.
            migrationBuilder.Sql(
                """
                UPDATE crm.pipeline_stages
                SET color = CASE color_index
                    WHEN 0 THEN '#e6ecf5'
                    WHEN 1 THEN '#c9d7eb'
                    WHEN 2 THEN '#93b2dc'
                    WHEN 3 THEN '#3866a0'
                    WHEN 4 THEN '#2453be'
                    ELSE '#e6ecf5'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                schema: "crm",
                table: "pipeline_stages",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_pipeline_stages_color",
                schema: "crm",
                table: "pipeline_stages",
                sql: "\"color\" ~ '^#[0-9a-f]{6}$'");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pipeline_stages_color_index",
                schema: "crm",
                table: "pipeline_stages");

            migrationBuilder.DropColumn(
                name: "color_index",
                schema: "crm",
                table: "pipeline_stages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_pipeline_stages_color",
                schema: "crm",
                table: "pipeline_stages");

            migrationBuilder.AddColumn<short>(
                name: "color_index",
                schema: "crm",
                table: "pipeline_stages",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            // Data step: the five ramp colours return to their step; a custom colour has no step and becomes the
            // lightest one, because the old column cannot hold a free colour.
            migrationBuilder.Sql(
                """
                UPDATE crm.pipeline_stages
                SET color_index = CASE lower(color)
                    WHEN '#e6ecf5' THEN 0
                    WHEN '#c9d7eb' THEN 1
                    WHEN '#93b2dc' THEN 2
                    WHEN '#3866a0' THEN 3
                    WHEN '#2453be' THEN 4
                    ELSE 0
                END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_pipeline_stages_color_index",
                schema: "crm",
                table: "pipeline_stages",
                sql: "\"color_index\" BETWEEN 0 AND 4");

            migrationBuilder.DropColumn(
                name: "color",
                schema: "crm",
                table: "pipeline_stages");
        }
    }
}
