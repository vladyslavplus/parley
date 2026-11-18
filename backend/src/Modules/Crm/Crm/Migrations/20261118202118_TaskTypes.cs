using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class TaskTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "type",
                schema: "crm",
                table: "tasks",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "todo");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tasks_type",
                schema: "crm",
                table: "tasks",
                sql: "\"type\" IN ('call', 'email', 'meeting', 'todo')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tasks_type",
                schema: "crm",
                table: "tasks");

            migrationBuilder.DropColumn(
                name: "type",
                schema: "crm",
                table: "tasks");
        }
    }
}
