using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Campaigns.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignTargetingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "targeting_model_name",
                schema: "campaigns",
                table: "campaigns",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "targeting_model_version",
                schema: "campaigns",
                table: "campaigns",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "targeting_provenance",
                schema: "campaigns",
                table: "campaigns",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "targeting_model_name",
                schema: "campaigns",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "targeting_model_version",
                schema: "campaigns",
                table: "campaigns");

            migrationBuilder.DropColumn(
                name: "targeting_provenance",
                schema: "campaigns",
                table: "campaigns");
        }
    }
}
