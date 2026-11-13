using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Insights.Migrations
{
    /// <inheritdoc />
    public partial class AddModelAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_available",
                schema: "insights",
                table: "model_versions",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_available",
                schema: "insights",
                table: "model_versions");
        }
    }
}
