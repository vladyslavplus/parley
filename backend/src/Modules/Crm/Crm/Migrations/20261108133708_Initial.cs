using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crm");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "activities",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: true),
                    country = table.Column<string>(type: "char(2)", nullable: true),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    custom_fields = table.Column<string>(type: "jsonb", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    name_normalized = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, computedColumnSql: "lower(\"name\")", stored: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple'::regconfig, coalesce(\"name\", '') || ' ' || coalesce(\"domain\", ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contacts",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    email_normalized = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    job_title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "char(2)", nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    custom_fields = table.Column<string>(type: "jsonb", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    name_normalized = table.Column<string>(type: "text", nullable: false, computedColumnSql: "lower(\"first_name\" || ' ' || coalesce(\"last_name\", ''))", stored: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple'::regconfig, coalesce(\"first_name\", '') || ' ' || coalesce(\"last_name\", '') || ' ' || coalesce(\"email\", '') || ' ' || coalesce(\"phone\", ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custom_field_definitions",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    key = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    field_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    options = table.Column<string>(type: "jsonb", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    archived_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_field_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "crm",
                columns: table => new
                {
                    consumer = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_messages", x => new { x.consumer, x.event_id });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    dead_lettered_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    color_index = table.Column<short>(type: "smallint", nullable: false),
                    name_normalized = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, computedColumnSql: "lower(\"name\")", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                    table.CheckConstraint("ck_tags_color_index", "\"color_index\" BETWEEN 0 AND 4");
                });

            migrationBuilder.CreateTable(
                name: "contact_tags",
                schema: "crm",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contact_tags", x => new { x.organization_id, x.contact_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_contact_tags_contacts_contact_id",
                        column: x => x.contact_id,
                        principalSchema: "crm",
                        principalTable: "contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contact_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "crm",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activities_organization_id_contact_id_occurred_at",
                schema: "crm",
                table: "activities",
                columns: new[] { "organization_id", "contact_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ux_activities_organization_id_source_event_id",
                schema: "crm",
                table: "activities",
                columns: new[] { "organization_id", "source_event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_companies_custom_fields",
                schema: "crm",
                table: "companies",
                column: "custom_fields")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_name_normalized_trgm",
                schema: "crm",
                table: "companies",
                column: "name_normalized")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_organization_id_created_at_id",
                schema: "crm",
                table: "companies",
                columns: new[] { "organization_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_organization_id_id",
                schema: "crm",
                table: "companies",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_organization_id_name_normalized_id",
                schema: "crm",
                table: "companies",
                columns: new[] { "organization_id", "name_normalized", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_organization_id_owner_user_id_id",
                schema: "crm",
                table: "companies",
                columns: new[] { "organization_id", "owner_user_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_companies_search_vector",
                schema: "crm",
                table: "companies",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_contact_tags_contact_id",
                schema: "crm",
                table: "contact_tags",
                column: "contact_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_tags_organization_id_tag_id",
                schema: "crm",
                table: "contact_tags",
                columns: new[] { "organization_id", "tag_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contact_tags_tag_id",
                schema: "crm",
                table: "contact_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_contacts_custom_fields",
                schema: "crm",
                table: "contacts",
                column: "custom_fields")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_contacts_email_normalized_trgm",
                schema: "crm",
                table: "contacts",
                column: "email_normalized")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_contacts_name_normalized_trgm",
                schema: "crm",
                table: "contacts",
                column: "name_normalized")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_contacts_organization_id_created_at_id",
                schema: "crm",
                table: "contacts",
                columns: new[] { "organization_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_contacts_organization_id_id",
                schema: "crm",
                table: "contacts",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_contacts_organization_id_name_normalized_id",
                schema: "crm",
                table: "contacts",
                columns: new[] { "organization_id", "name_normalized", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_contacts_organization_id_owner_user_id_id",
                schema: "crm",
                table: "contacts",
                columns: new[] { "organization_id", "owner_user_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_contacts_search_vector",
                schema: "crm",
                table: "contacts",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ux_contacts_organization_id_email_normalized_live",
                schema: "crm",
                table: "contacts",
                columns: new[] { "organization_id", "email_normalized" },
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_custom_field_definitions_organization_id_entity_type_key",
                schema: "crm",
                table: "custom_field_definitions",
                columns: new[] { "organization_id", "entity_type", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_next_attempt_at",
                schema: "crm",
                table: "outbox_messages",
                column: "next_attempt_at",
                filter: "\"processed_at\" IS NULL AND \"dead_lettered_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tags_organization_id_id",
                schema: "crm",
                table: "tags",
                columns: new[] { "organization_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ux_tags_organization_id_name_normalized",
                schema: "crm",
                table: "tags",
                columns: new[] { "organization_id", "name_normalized" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activities",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "contact_tags",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "custom_field_definitions",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "contacts",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "crm");
        }
    }
}
