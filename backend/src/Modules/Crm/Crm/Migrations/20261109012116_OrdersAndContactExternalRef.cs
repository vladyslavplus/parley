using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class OrdersAndContactExternalRef : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_ref",
                schema: "crm",
                table: "contacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_order_at",
                schema: "crm",
                table: "contacts",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_ref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ordered_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    line_count = table.Column<int>(type: "integer", nullable: false),
                    is_cancellation = table.Column<bool>(type: "boolean", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_contacts_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_contacts_organization_id_source_external_ref",
                schema: "crm",
                table: "contacts",
                columns: new[] { "organization_id", "source", "external_ref" },
                unique: true,
                filter: "\"external_ref\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_orders_contact_id",
                schema: "crm",
                table: "orders",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_organization_id_contact_id_ordered_at",
                schema: "crm",
                table: "orders",
                columns: new[] { "organization_id", "contact_id", "ordered_at" });

            migrationBuilder.CreateIndex(
                name: "ux_orders_organization_id_source_external_ref",
                schema: "crm",
                table: "orders",
                columns: new[] { "organization_id", "source", "external_ref" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "orders",
                schema: "crm");

            migrationBuilder.DropIndex(
                name: "ux_contacts_organization_id_source_external_ref",
                schema: "crm",
                table: "contacts");

            migrationBuilder.DropColumn(
                name: "external_ref",
                schema: "crm",
                table: "contacts");

            migrationBuilder.DropColumn(
                name: "last_order_at",
                schema: "crm",
                table: "contacts");
        }
    }
}
