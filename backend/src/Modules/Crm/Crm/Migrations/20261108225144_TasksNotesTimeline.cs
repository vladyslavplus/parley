using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class TasksNotesTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "note_id",
                schema: "crm",
                table: "activities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "task_id",
                schema: "crm",
                table: "activities",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.id);
                    table.CheckConstraint("ck_notes_subject_single", "num_nonnulls(\"contact_id\", \"deal_id\", \"company_id\") = 1");
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_normalized = table.Column<string>(type: "text", nullable: false, computedColumnSql: "lower(\"title\")", stored: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    due_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    assignee_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reminder_sent_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activities_organization_id_company_id_occurred_at",
                schema: "crm",
                table: "activities",
                columns: new[] { "organization_id", "company_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notes_organization_id_company_id_created_at_id",
                schema: "crm",
                table: "notes",
                columns: new[] { "organization_id", "company_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_notes_organization_id_contact_id_created_at_id",
                schema: "crm",
                table: "notes",
                columns: new[] { "organization_id", "contact_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_notes_organization_id_deal_id_created_at_id",
                schema: "crm",
                table: "notes",
                columns: new[] { "organization_id", "deal_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_due_at_id_open",
                schema: "crm",
                table: "tasks",
                columns: new[] { "due_at", "id" },
                filter: "\"completed_at\" IS NULL AND \"deleted_at\" IS NULL AND \"reminder_sent_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_organization_id_assignee_user_id_due_at_id",
                schema: "crm",
                table: "tasks",
                columns: new[] { "organization_id", "assignee_user_id", "due_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_organization_id_created_at_id",
                schema: "crm",
                table: "tasks",
                columns: new[] { "organization_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_tasks_organization_id_title_normalized_id",
                schema: "crm",
                table: "tasks",
                columns: new[] { "organization_id", "title_normalized", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notes",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "tasks",
                schema: "crm");

            migrationBuilder.DropIndex(
                name: "ix_activities_organization_id_company_id_occurred_at",
                schema: "crm",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "note_id",
                schema: "crm",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "task_id",
                schema: "crm",
                table: "activities");
        }
    }
}
