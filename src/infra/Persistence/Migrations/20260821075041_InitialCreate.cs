using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dictionary_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dictionary_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grammar_rules",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_id = table.Column<string>(type: "text", nullable: false),
                    pattern = table.Column<string>(type: "text", nullable: false),
                    jlpt_level = table.Column<string>(type: "text", nullable: true),
                    meaning_vi = table.Column<string>(type: "text", nullable: true),
                    meaning_en = table.Column<string>(type: "text", nullable: true),
                    formation = table.Column<string>(type: "text", nullable: true),
                    examples = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb"),
                    matcher_metadata = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    source_metadata = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grammar_rules", x => x.id);
                    table.CheckConstraint("chk_grammar_rules_jlpt", "jlpt_level IS NULL OR jlpt_level IN ('N5', 'N4', 'N3', 'N2', 'N1')");
                });

            migrationBuilder.CreateTable(
                name: "kanji_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    canonical_id = table.Column<string>(type: "text", nullable: false),
                    character = table.Column<string>(type: "text", nullable: false),
                    stroke_count = table.Column<short>(type: "smallint", nullable: true),
                    grade = table.Column<short>(type: "smallint", nullable: true),
                    jlpt_level = table.Column<short>(type: "smallint", nullable: true),
                    jlpt_provenance = table.Column<string>(type: "text", nullable: true),
                    frequency = table.Column<short>(type: "smallint", nullable: true),
                    unicode_codepoint = table.Column<string>(type: "text", nullable: false),
                    radical_number = table.Column<short>(type: "smallint", nullable: true),
                    han_viet = table.Column<string>(type: "text", nullable: true),
                    on_readings = table.Column<string[]>(type: "text[]", nullable: true),
                    kun_readings = table.Column<string[]>(type: "text[]", nullable: true),
                    meanings_en = table.Column<string[]>(type: "text[]", nullable: true),
                    meanings_vi = table.Column<string[]>(type: "text[]", nullable: true),
                    nanori_readings = table.Column<string[]>(type: "text[]", nullable: true),
                    source_metadata = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kanji_records", x => x.id);
                    table.CheckConstraint("chk_kanji_records_jlpt_provenance", "jlpt_provenance IS NULL OR jlpt_provenance IN ('authoritative', 'derived', 'approximate')");
                });

            migrationBuilder.CreateTable(
                name: "knowledge_releases",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    version = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_releases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source_manifests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_name = table.Column<string>(type: "text", nullable: false),
                    publisher = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<string>(type: "text", nullable: false),
                    license = table.Column<string>(type: "text", nullable: true),
                    attribution = table.Column<string>(type: "text", nullable: true),
                    checksum = table.Column<string>(type: "text", nullable: true),
                    record_count = table.Column<int>(type: "integer", nullable: true),
                    rejected_count = table.Column<int>(type: "integer", nullable: true),
                    transformation_id = table.Column<string>(type: "text", nullable: true),
                    validation_result = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_manifests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    email_verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dictionary_senses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entry_id = table.Column<long>(type: "bigint", nullable: false),
                    sense_key = table.Column<string>(type: "text", nullable: false),
                    part_of_speech = table.Column<string[]>(type: "text[]", nullable: true),
                    position = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dictionary_senses", x => x.id);
                    table.ForeignKey(
                        name: "FK_dictionary_senses_dictionary_entries_entry_id",
                        column: x => x.entry_id,
                        principalTable: "dictionary_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "readings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entry_id = table.Column<long>(type: "bigint", nullable: false),
                    reading = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<short>(type: "smallint", nullable: true),
                    is_common = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    info = table.Column<string[]>(type: "text[]", nullable: true),
                    restricted_to_forms = table.Column<string[]>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_readings", x => x.id);
                    table.ForeignKey(
                        name: "FK_readings_dictionary_entries_entry_id",
                        column: x => x.entry_id,
                        principalTable: "dictionary_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "written_forms",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entry_id = table.Column<long>(type: "bigint", nullable: false),
                    form = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<short>(type: "smallint", nullable: true),
                    is_common = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    info = table.Column<string[]>(type: "text[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_written_forms", x => x.id);
                    table.ForeignKey(
                        name: "FK_written_forms_dictionary_entries_entry_id",
                        column: x => x.entry_id,
                        principalTable: "dictionary_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resource_revisions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    release_id = table.Column<long>(type: "bigint", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    resource_id = table.Column<long>(type: "bigint", nullable: false),
                    revision_data = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resource_revisions", x => x.id);
                    table.CheckConstraint("chk_resource_revisions_type", "resource_type IN ('dictionary', 'kanji', 'grammar')");
                    table.ForeignKey(
                        name: "FK_resource_revisions_knowledge_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "knowledge_releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_release_manifests",
                columns: table => new
                {
                    release_id = table.Column<long>(type: "bigint", nullable: false),
                    manifest_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_release_manifests", x => new { x.release_id, x.manifest_id });
                    table.ForeignKey(
                        name: "FK_knowledge_release_manifests_knowledge_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "knowledge_releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_knowledge_release_manifests_source_manifests_manifest_id",
                        column: x => x.manifest_id,
                        principalTable: "source_manifests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "source_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    manifest_id = table.Column<long>(type: "bigint", nullable: false),
                    source_identity = table.Column<string>(type: "text", nullable: false),
                    record_data = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_source_records_source_manifests_manifest_id",
                        column: x => x.manifest_id,
                        principalTable: "source_manifests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_references",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    resource_id = table.Column<long>(type: "bigint", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_references", x => x.id);
                    table.CheckConstraint("chk_learning_references_type", "resource_type IN ('dictionary', 'grammar')");
                    table.ForeignKey(
                        name: "FK_learning_references_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_external_logins",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    provider_user_id = table.Column<string>(type: "text", nullable: false),
                    provider_email = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_external_logins", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_external_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sense_applicabilities",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sense_id = table.Column<long>(type: "bigint", nullable: false),
                    written_form_id = table.Column<long>(type: "bigint", nullable: true),
                    reading_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sense_applicabilities", x => x.id);
                    table.CheckConstraint("chk_sense_applicabilities_has_target", "written_form_id IS NOT NULL OR reading_id IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_sense_applicabilities_dictionary_senses_sense_id",
                        column: x => x.sense_id,
                        principalTable: "dictionary_senses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sense_applicabilities_readings_reading_id",
                        column: x => x.reading_id,
                        principalTable: "readings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sense_applicabilities_written_forms_written_form_id",
                        column: x => x.written_form_id,
                        principalTable: "written_forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "editorial_mappings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_record_id = table.Column<long>(type: "bigint", nullable: false),
                    target_resource_type = table.Column<string>(type: "text", nullable: false),
                    target_resource_id = table.Column<long>(type: "bigint", nullable: false),
                    mapping_type = table.Column<string>(type: "text", nullable: false),
                    decision_reason = table.Column<string>(type: "text", nullable: true),
                    reviewed_by = table.Column<string>(type: "text", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_editorial_mappings", x => x.id);
                    table.CheckConstraint("chk_editorial_mappings_resource_type", "target_resource_type IN ('dictionary', 'kanji', 'grammar')");
                    table.CheckConstraint("chk_editorial_mappings_type", "mapping_type IN ('link', 'merge', 'split', 'retire')");
                    table.ForeignKey(
                        name: "FK_editorial_mappings_source_records_source_record_id",
                        column: x => x.source_record_id,
                        principalTable: "source_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "localized_glosses",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sense_id = table.Column<long>(type: "bigint", nullable: false),
                    language_tag = table.Column<string>(type: "text", nullable: false),
                    gloss_text = table.Column<string>(type: "text", nullable: false),
                    source_record_id = table.Column<long>(type: "bigint", nullable: true),
                    review_status = table.Column<string>(type: "text", nullable: true),
                    position = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_localized_glosses", x => x.id);
                    table.CheckConstraint("chk_localized_glosses_language_tag", "language_tag IN ('vi', 'en')");
                    table.ForeignKey(
                        name: "FK_localized_glosses_dictionary_senses_sense_id",
                        column: x => x.sense_id,
                        principalTable: "dictionary_senses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_localized_glosses_source_records_source_record_id",
                        column: x => x.source_record_id,
                        principalTable: "source_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "uq_dictionary_entries_canonical_id",
                table: "dictionary_entries",
                column: "canonical_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_dictionary_senses_entry_id",
                table: "dictionary_senses",
                column: "entry_id");

            migrationBuilder.CreateIndex(
                name: "uq_dictionary_senses_entry_key",
                table: "dictionary_senses",
                columns: new[] { "entry_id", "sense_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_editorial_mappings_target",
                table: "editorial_mappings",
                columns: new[] { "target_resource_type", "target_resource_id" });

            migrationBuilder.CreateIndex(
                name: "uq_editorial_mappings_source_target",
                table: "editorial_mappings",
                columns: new[] { "source_record_id", "target_resource_type", "target_resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_grammar_rules_jlpt",
                table: "grammar_rules",
                column: "jlpt_level",
                filter: "jlpt_level IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_grammar_rules_canonical_id",
                table: "grammar_rules",
                column: "canonical_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_kanji_records_jlpt",
                table: "kanji_records",
                column: "jlpt_level",
                filter: "jlpt_level IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_kanji_records_canonical_id",
                table: "kanji_records",
                column: "canonical_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_kanji_records_character",
                table: "kanji_records",
                column: "character",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_release_manifests_manifest_id",
                table: "knowledge_release_manifests",
                column: "manifest_id");

            migrationBuilder.CreateIndex(
                name: "uq_knowledge_releases_current",
                table: "knowledge_releases",
                column: "is_current",
                unique: true,
                filter: "is_current = TRUE");

            migrationBuilder.CreateIndex(
                name: "uq_knowledge_releases_version",
                table: "knowledge_releases",
                column: "version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_learning_references_user_active",
                table: "learning_references",
                columns: new[] { "user_id", "resource_type" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_learning_references_active",
                table: "learning_references",
                columns: new[] { "user_id", "resource_type", "resource_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_localized_glosses_sense_lang",
                table: "localized_glosses",
                columns: new[] { "sense_id", "language_tag" });

            migrationBuilder.CreateIndex(
                name: "IX_localized_glosses_source_record_id",
                table: "localized_glosses",
                column: "source_record_id");

            migrationBuilder.CreateIndex(
                name: "idx_readings_entry_id",
                table: "readings",
                column: "entry_id");

            migrationBuilder.CreateIndex(
                name: "idx_readings_reading",
                table: "readings",
                column: "reading");

            migrationBuilder.CreateIndex(
                name: "uq_readings_entry_reading",
                table: "readings",
                columns: new[] { "entry_id", "reading" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_resource_revisions_release_resource",
                table: "resource_revisions",
                columns: new[] { "release_id", "resource_type", "resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sense_applicabilities_reading_id",
                table: "sense_applicabilities",
                column: "reading_id");

            migrationBuilder.CreateIndex(
                name: "IX_sense_applicabilities_written_form_id",
                table: "sense_applicabilities",
                column: "written_form_id");

            migrationBuilder.CreateIndex(
                name: "uq_sense_applicabilities_combo",
                table: "sense_applicabilities",
                columns: new[] { "sense_id", "written_form_id", "reading_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_source_manifests_name_version",
                table: "source_manifests",
                columns: new[] { "source_name", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_source_records_identity",
                table: "source_records",
                column: "source_identity");

            migrationBuilder.CreateIndex(
                name: "uq_source_records_manifest_identity",
                table: "source_records",
                columns: new[] { "manifest_id", "source_identity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_external_logins_user_id",
                table: "user_external_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_external_logins_provider_uid",
                table: "user_external_logins",
                columns: new[] { "provider", "provider_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_written_forms_entry_id",
                table: "written_forms",
                column: "entry_id");

            migrationBuilder.CreateIndex(
                name: "idx_written_forms_form",
                table: "written_forms",
                column: "form");

            migrationBuilder.CreateIndex(
                name: "uq_written_forms_entry_form",
                table: "written_forms",
                columns: new[] { "entry_id", "form" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "editorial_mappings");

            migrationBuilder.DropTable(
                name: "grammar_rules");

            migrationBuilder.DropTable(
                name: "kanji_records");

            migrationBuilder.DropTable(
                name: "knowledge_release_manifests");

            migrationBuilder.DropTable(
                name: "learning_references");

            migrationBuilder.DropTable(
                name: "localized_glosses");

            migrationBuilder.DropTable(
                name: "resource_revisions");

            migrationBuilder.DropTable(
                name: "sense_applicabilities");

            migrationBuilder.DropTable(
                name: "user_external_logins");

            migrationBuilder.DropTable(
                name: "source_records");

            migrationBuilder.DropTable(
                name: "knowledge_releases");

            migrationBuilder.DropTable(
                name: "dictionary_senses");

            migrationBuilder.DropTable(
                name: "readings");

            migrationBuilder.DropTable(
                name: "written_forms");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "source_manifests");

            migrationBuilder.DropTable(
                name: "dictionary_entries");
        }
    }
}
