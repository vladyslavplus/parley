using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class ContactCreatedCompanyBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data step: contact-created entries written before the company was recorded get the contact's company.
            // Only rows still without a company are touched, so a second run changes nothing.
            migrationBuilder.Sql(
                """
                UPDATE crm.activities AS a
                SET company_id = c.company_id
                FROM crm.contacts AS c
                WHERE a.contact_id = c.id
                  AND a.type = 'contact_created'
                  AND a.company_id IS NULL
                  AND c.company_id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The backfilled values cannot be told apart from values written later, so nothing is reverted.
        }
    }
}
