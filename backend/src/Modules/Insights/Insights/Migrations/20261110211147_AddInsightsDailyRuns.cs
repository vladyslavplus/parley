using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Insights.Migrations
{
    /// <inheritdoc />
    public partial class AddInsightsDailyRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_runs",
                schema: "insights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    rfm_completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    priority_completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_runs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_daily_runs_organization_id_as_of",
                schema: "insights",
                table: "daily_runs",
                columns: new[] { "organization_id", "as_of" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_runs",
                schema: "insights");
        }
    }
}
