using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoSeedState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "demo_seed_states",
                schema: "identity",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seeded_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_demo_seed_states", x => x.organization_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demo_seed_states",
                schema: "identity");
        }
    }
}
