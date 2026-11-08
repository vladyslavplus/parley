using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Parley.Identity.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "identity",
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
                name: "organizations",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    default_locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "identity",
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
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teams",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_normalized = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, computedColumnSql: "lower(\"name\")", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    theme = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    last_active_organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_platform_operator = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_theme", "\"theme\" IN ('system', 'light', 'dark')");
                    table.ForeignKey(
                        name: "fk_users_organizations_last_active_organization_id",
                        column: x => x.last_active_organization_id,
                        principalSchema: "identity",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "identity",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission });
                    table.CheckConstraint("ck_role_permissions_scope", "\"scope\" IS NULL OR \"scope\" IN ('own', 'team', 'org')");
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invitations",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    email_normalized = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitations_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invitations_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "identity",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "memberships",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memberships", x => x.id);
                    table.CheckConstraint("ck_memberships_status", "\"status\" IN ('active', 'deactivated')");
                    table.ForeignKey(
                        name: "fk_memberships_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_memberships_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "identity",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_memberships_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    replaced_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "identity",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "roles",
                columns: new[] { "id", "code", "is_system", "organization_id" },
                values: new object[,]
                {
                    { new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "admin", true, null },
                    { new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "sales_manager", true, null },
                    { new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "sales_rep", true, null },
                    { new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "marketer", true, null },
                    { new Guid("0192f5a0-7b1c-7000-8000-000000000005"), "viewer", true, null }
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permissions",
                columns: new[] { "permission", "role_id", "scope" },
                values: new object[,]
                {
                    { "activities.read", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "activities.write", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "audit.read", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "campaigns.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "campaigns.read", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "campaigns.send", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "companies.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "companies.read", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "companies.write", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "contacts.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "contacts.export", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "contacts.read", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "contacts.write", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "customfields.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "dashboards.view", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "deals.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "deals.read", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "deals.write", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "insights.ml.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "insights.ml.run", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "insights.ml.view", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "insights.view", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "orders.read", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "org.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "pipelines.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "tags.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "tasks.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "tasks.read", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "tasks.write", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), "org" },
                    { "teams.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "users.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000001"), null },
                    { "activities.read", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "activities.write", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "campaigns.read", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), null },
                    { "companies.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "companies.read", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "companies.write", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "contacts.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "contacts.export", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "contacts.read", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "contacts.write", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "dashboards.view", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), null },
                    { "deals.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "deals.read", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "deals.write", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "insights.ml.view", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), null },
                    { "insights.view", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), null },
                    { "orders.read", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "tags.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), null },
                    { "tasks.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "tasks.read", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "tasks.write", new Guid("0192f5a0-7b1c-7000-8000-000000000002"), "team" },
                    { "activities.read", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "activities.write", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "companies.read", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "companies.write", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "contacts.read", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "contacts.write", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "dashboards.view", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), null },
                    { "deals.read", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "deals.write", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "insights.view", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), null },
                    { "orders.read", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "tasks.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "tasks.read", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "tasks.write", new Guid("0192f5a0-7b1c-7000-8000-000000000003"), "own" },
                    { "activities.read", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "org" },
                    { "activities.write", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "org" },
                    { "campaigns.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), null },
                    { "campaigns.read", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), null },
                    { "campaigns.send", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), null },
                    { "companies.read", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "org" },
                    { "companies.write", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "org" },
                    { "contacts.export", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "org" },
                    { "contacts.read", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "org" },
                    { "contacts.write", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "org" },
                    { "dashboards.view", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), null },
                    { "deals.read", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "org" },
                    { "insights.ml.run", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), null },
                    { "insights.ml.view", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), null },
                    { "insights.view", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), null },
                    { "orders.read", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "org" },
                    { "tags.manage", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), null },
                    { "tasks.delete", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "own" },
                    { "tasks.read", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "own" },
                    { "tasks.write", new Guid("0192f5a0-7b1c-7000-8000-000000000004"), "own" },
                    { "activities.read", new Guid("0192f5a0-7b1c-7000-8000-000000000005"), "team" },
                    { "campaigns.read", new Guid("0192f5a0-7b1c-7000-8000-000000000005"), null },
                    { "companies.read", new Guid("0192f5a0-7b1c-7000-8000-000000000005"), "team" },
                    { "contacts.read", new Guid("0192f5a0-7b1c-7000-8000-000000000005"), "team" },
                    { "dashboards.view", new Guid("0192f5a0-7b1c-7000-8000-000000000005"), null },
                    { "deals.read", new Guid("0192f5a0-7b1c-7000-8000-000000000005"), "team" },
                    { "insights.view", new Guid("0192f5a0-7b1c-7000-8000-000000000005"), null },
                    { "orders.read", new Guid("0192f5a0-7b1c-7000-8000-000000000005"), "team" },
                    { "tasks.read", new Guid("0192f5a0-7b1c-7000-8000-000000000005"), "team" }
                });

            migrationBuilder.CreateIndex(
                name: "ux_audit_events_source_event_id",
                schema: "identity",
                table: "audit_events",
                column: "source_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitations_role_id",
                schema: "identity",
                table: "invitations",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_invitations_team_id",
                schema: "identity",
                table: "invitations",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ux_invitations_organization_id_email_pending",
                schema: "identity",
                table: "invitations",
                columns: new[] { "organization_id", "email_normalized" },
                unique: true,
                filter: "\"accepted_at\" IS NULL AND \"revoked_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_invitations_token_hash",
                schema: "identity",
                table: "invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_memberships_organization_id_team_id",
                schema: "identity",
                table: "memberships",
                columns: new[] { "organization_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "ix_memberships_role_id",
                schema: "identity",
                table: "memberships",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_memberships_team_id",
                schema: "identity",
                table: "memberships",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_memberships_user_id",
                schema: "identity",
                table: "memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_memberships_organization_id_user_id",
                schema: "identity",
                table: "memberships",
                columns: new[] { "organization_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_organizations_slug",
                schema: "identity",
                table: "organizations",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_next_attempt_at",
                schema: "identity",
                table: "outbox_messages",
                column: "next_attempt_at",
                filter: "\"processed_at\" IS NULL AND \"dead_lettered_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "identity",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "identity",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_roles_organization_id_code",
                schema: "identity",
                table: "roles",
                columns: new[] { "organization_id", "code" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ux_teams_organization_id_name_normalized",
                schema: "identity",
                table: "teams",
                columns: new[] { "organization_id", "name_normalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                schema: "identity",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                schema: "identity",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_users_last_active_organization_id",
                schema: "identity",
                table: "users",
                column: "last_active_organization_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "users",
                column: "normalized_user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "invitations",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "memberships",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_claims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "teams",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "identity");
        }
    }
}
