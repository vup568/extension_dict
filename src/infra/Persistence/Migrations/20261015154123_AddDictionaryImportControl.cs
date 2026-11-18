using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDictionaryImportControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_no_kanji",
                table: "readings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "dictionary_import_runs",
                columns: table => new
                {
                    run_id = table.Column<string>(type: "text", nullable: false),
                    target_database = table.Column<string>(type: "text", nullable: false),
                    database_oid = table.Column<long>(type: "bigint", nullable: false),
                    source_lock_hash = table.Column<string>(type: "text", nullable: false),
                    fingerprint_hash = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    facts_hash = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dictionary_import_runs", x => x.run_id);
                    table.CheckConstraint("ck_import_state", "state IN ('new','loading','failed','cancelled','imported','validated','approved_for_local_test','sealed')");
                });

            migrationBuilder.CreateTable(
                name: "dictionary_import_checkpoints",
                columns: table => new
                {
                    run_id = table.Column<string>(type: "text", nullable: false),
                    source_id = table.Column<string>(type: "text", nullable: false),
                    manifest_id = table.Column<long>(type: "bigint", nullable: false),
                    accepted_records = table.Column<long>(type: "bigint", nullable: false),
                    last_source_identity = table.Column<string>(type: "text", nullable: false),
                    batch_chain_hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dictionary_import_checkpoints", x => new { x.run_id, x.source_id });
                    table.CheckConstraint("ck_import_checkpoint_count", "accepted_records >= 0");
                    table.ForeignKey(
                        name: "FK_dictionary_import_checkpoints_dictionary_import_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "dictionary_import_runs",
                        principalColumn: "run_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dictionary_import_checkpoints_source_manifests_manifest_id",
                        column: x => x.manifest_id,
                        principalTable: "source_manifests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dictionary_import_checkpoints_manifest_id",
                table: "dictionary_import_checkpoints",
                column: "manifest_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dictionary_import_runs_target_database",
                table: "dictionary_import_runs",
                column: "target_database",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dictionary_import_checkpoints");

            migrationBuilder.DropTable(
                name: "dictionary_import_runs");

            migrationBuilder.DropColumn(
                name: "is_no_kanji",
                table: "readings");
        }
    }
}
