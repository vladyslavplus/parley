using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Parley.Crm.Migrations
{
    /// <inheritdoc />
    public partial class DealsAndPipelines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                schema: "crm",
                table: "activities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deal_id",
                schema: "crm",
                table: "activities",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "deal_stage_history",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    deal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_stage_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deal_stage_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pipelines",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipelines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pipeline_stages",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    pipeline_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_normalized = table.Column<string>(type: "text", nullable: false, computedColumnSql: "lower(\"name\")", stored: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    probability_bps = table.Column<int>(type: "integer", nullable: false),
                    color_index = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pipeline_stages", x => x.id);
                    table.CheckConstraint("ck_pipeline_stages_color_index", "\"color_index\" BETWEEN 0 AND 4");
                    table.CheckConstraint("ck_pipeline_stages_kind", "\"kind\" IN ('open', 'won', 'lost')");
                    table.CheckConstraint("ck_pipeline_stages_probability_bps", "\"probability_bps\" BETWEEN 0 AND 10000");
                    table.ForeignKey(
                        name: "fk_pipeline_stages_pipelines_pipeline_id",
                        column: x => x.pipeline_id,
                        principalSchema: "crm",
                        principalTable: "pipelines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deals",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    pipeline_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_normalized = table.Column<string>(type: "text", nullable: false, computedColumnSql: "lower(\"title\")", stored: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    contact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    lost_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expected_close_on = table.Column<DateOnly>(type: "date", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    board_position = table.Column<long>(type: "bigint", nullable: false),
                    custom_fields = table.Column<string>(type: "jsonb", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple'::regconfig, coalesce(\"title\", ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deals", x => x.id);
                    table.CheckConstraint("ck_deals_status", "\"status\" IN ('open', 'won', 'lost')");
                    table.ForeignKey(
                        name: "fk_deals_pipeline_stages_stage_id",
                        column: x => x.stage_id,
                        principalSchema: "crm",
                        principalTable: "pipeline_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_deals_pipelines_pipeline_id",
                        column: x => x.pipeline_id,
                        principalSchema: "crm",
                        principalTable: "pipelines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activities_organization_id_deal_id_occurred_at",
                schema: "crm",
                table: "activities",
                columns: new[] { "organization_id", "deal_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_deal_stage_history_organization_id_deal_id_changed_at",
                schema: "crm",
                table: "deal_stage_history",
                columns: new[] { "organization_id", "deal_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_deals_custom_fields",
                schema: "crm",
                table: "deals",
                column: "custom_fields")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_deals_organization_id_amount_id",
                schema: "crm",
                table: "deals",
                columns: new[] { "organization_id", "amount", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_deals_organization_id_created_at_id",
                schema: "crm",
                table: "deals",
                columns: new[] { "organization_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_deals_organization_id_owner_user_id_status",
                schema: "crm",
                table: "deals",
                columns: new[] { "organization_id", "owner_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_deals_organization_id_pipeline_id_stage_id_board_position",
                schema: "crm",
                table: "deals",
                columns: new[] { "organization_id", "pipeline_id", "stage_id", "board_position" });

            migrationBuilder.CreateIndex(
                name: "ix_deals_organization_id_title_normalized_id",
                schema: "crm",
                table: "deals",
                columns: new[] { "organization_id", "title_normalized", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_deals_pipeline_id",
                schema: "crm",
                table: "deals",
                column: "pipeline_id");

            migrationBuilder.CreateIndex(
                name: "ix_deals_search_vector",
                schema: "crm",
                table: "deals",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_deals_stage_id",
                schema: "crm",
                table: "deals",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_stages_pipeline_id",
                schema: "crm",
                table: "pipeline_stages",
                column: "pipeline_id");

            migrationBuilder.CreateIndex(
                name: "ux_pipeline_stages_organization_id_pipeline_id_name_normalized",
                schema: "crm",
                table: "pipeline_stages",
                columns: new[] { "organization_id", "pipeline_id", "name_normalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_pipeline_stages_organization_id_pipeline_id_position",
                schema: "crm",
                table: "pipeline_stages",
                columns: new[] { "organization_id", "pipeline_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_pipelines_organization_id_default",
                schema: "crm",
                table: "pipelines",
                column: "organization_id",
                unique: true,
                filter: "\"is_default\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deal_stage_history",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "deals",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "pipeline_stages",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "pipelines",
                schema: "crm");

            migrationBuilder.DropIndex(
                name: "ix_activities_organization_id_deal_id_occurred_at",
                schema: "crm",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "company_id",
                schema: "crm",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "deal_id",
                schema: "crm",
                table: "activities");
        }
    }
}
