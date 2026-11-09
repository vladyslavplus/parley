using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class PipelineSystemKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "system_key",
                schema: "crm",
                table: "pipelines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "system_key",
                schema: "crm",
                table: "pipeline_stages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "system_key",
                schema: "crm",
                table: "pipelines");

            migrationBuilder.DropColumn(
                name: "system_key",
                schema: "crm",
                table: "pipeline_stages");
        }
    }
}
