using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Campaigns.Migrations
{
    /// <inheritdoc />
    public partial class CampaignPrepareInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "prepare_as_of",
                schema: "campaigns",
                table: "campaigns",
                type: "timestamptz",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "prepare_as_of",
                schema: "campaigns",
                table: "campaigns");
        }
    }
}
