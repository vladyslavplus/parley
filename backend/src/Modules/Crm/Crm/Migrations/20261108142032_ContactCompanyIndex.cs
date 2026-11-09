using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class ContactCompanyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_contacts_organization_id_company_id_id",
                schema: "crm",
                table: "contacts",
                columns: new[] { "organization_id", "company_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_contacts_organization_id_company_id_id",
                schema: "crm",
                table: "contacts");
        }
    }
}
