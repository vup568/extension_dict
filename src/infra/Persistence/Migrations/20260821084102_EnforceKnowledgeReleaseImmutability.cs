using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceKnowledgeReleaseImmutability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_knowledge_release_manifests_knowledge_releases_release_id",
                table: "knowledge_release_manifests");

            migrationBuilder.DropForeignKey(
                name: "FK_resource_revisions_knowledge_releases_release_id",
                table: "resource_revisions");

            migrationBuilder.DropIndex(
                name: "uq_knowledge_releases_current",
                table: "knowledge_releases");

            migrationBuilder.CreateTable(
                name: "current_knowledge_release",
                columns: table => new
                {
                    singleton_id = table.Column<short>(type: "smallint", nullable: false),
                    release_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_current_knowledge_release", x => x.singleton_id);
                    table.CheckConstraint("chk_current_knowledge_release_singleton", "singleton_id = 1");
                    table.ForeignKey(
                        name: "FK_current_knowledge_release_knowledge_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "knowledge_releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_current_knowledge_release_release",
                table: "current_knowledge_release",
                column: "release_id",
                unique: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM knowledge_releases
                        WHERE is_current = TRUE AND published_at IS NULL
                    ) THEN
                        RAISE EXCEPTION 'A draft knowledge release cannot be migrated as current.'
                            USING ERRCODE = '23514';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM knowledge_releases AS releases
                        WHERE releases.published_at IS NOT NULL
                          AND NOT EXISTS (
                              SELECT 1
                              FROM knowledge_release_manifests AS memberships
                              WHERE memberships.release_id = releases.id
                          )
                    ) THEN
                        RAISE EXCEPTION 'Published legacy knowledge releases require at least one source manifest before immutability migration.'
                            USING ERRCODE = '23514';
                    END IF;
                END;
                $$;

                INSERT INTO current_knowledge_release (singleton_id, release_id)
                SELECT 1, id
                FROM knowledge_releases
                WHERE is_current = TRUE;
                """);

            migrationBuilder.DropColumn(
                name: "is_current",
                table: "knowledge_releases");

            migrationBuilder.AddForeignKey(
                name: "FK_knowledge_release_manifests_knowledge_releases_release_id",
                table: "knowledge_release_manifests",
                column: "release_id",
                principalTable: "knowledge_releases",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_resource_revisions_knowledge_releases_release_id",
                table: "resource_revisions",
                column: "release_id",
                principalTable: "knowledge_releases",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                COMMENT ON TABLE current_knowledge_release IS
                    'Singleton pointer to the published Knowledge Release used as current facts.';
                COMMENT ON COLUMN current_knowledge_release.release_id IS
                    'Published release selected by controlled publication.';

                CREATE FUNCTION guard_published_knowledge_release()
                RETURNS trigger AS $$
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        IF NEW.published_at IS NOT NULL THEN
                            RAISE EXCEPTION 'Knowledge releases must be created as drafts.'
                                USING ERRCODE = 'P0001';
                        END IF;

                        RETURN NEW;
                    END IF;

                    IF OLD.published_at IS NOT NULL THEN
                        RAISE EXCEPTION 'Published knowledge releases are immutable.'
                            USING ERRCODE = 'P0001';
                    END IF;

                    IF TG_OP = 'UPDATE'
                       AND NEW.published_at IS NOT NULL
                       AND (
                            NEW.version IS DISTINCT FROM OLD.version
                            OR NEW.description IS DISTINCT FROM OLD.description
                            OR NEW.created_at IS DISTINCT FROM OLD.created_at
                       ) THEN
                        RAISE EXCEPTION 'Publishing may only set published_at.'
                            USING ERRCODE = 'P0001';
                    END IF;

                    IF TG_OP = 'UPDATE'
                       AND NEW.published_at IS NOT NULL
                       AND OLD.published_at IS NULL
                       AND current_setting('jp_reading.controlled_publication', TRUE) IS DISTINCT FROM 'on' THEN
                        RAISE EXCEPTION 'Knowledge releases may only be published by controlled publication.'
                            USING ERRCODE = 'P0001';
                    END IF;

                    IF TG_OP = 'UPDATE'
                       AND NEW.published_at IS NOT NULL
                       AND OLD.published_at IS NULL
                       AND NOT EXISTS (
                            SELECT 1
                            FROM knowledge_release_manifests
                            WHERE release_id = OLD.id
                       ) THEN
                        RAISE EXCEPTION 'A knowledge release requires at least one source manifest before publication.'
                            USING ERRCODE = 'P0001';
                    END IF;

                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_guard_published_knowledge_release
                    BEFORE INSERT OR UPDATE OR DELETE ON knowledge_releases
                    FOR EACH ROW
                    EXECUTE FUNCTION guard_published_knowledge_release();

                CREATE FUNCTION guard_published_release_child()
                RETURNS trigger AS $$
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        PERFORM 1
                        FROM knowledge_releases
                        WHERE id = NEW.release_id
                        FOR UPDATE;
                    ELSIF TG_OP = 'DELETE' THEN
                        PERFORM 1
                        FROM knowledge_releases
                        WHERE id = OLD.release_id
                        FOR UPDATE;
                    ELSE
                        PERFORM 1
                        FROM knowledge_releases
                        WHERE id IN (OLD.release_id, NEW.release_id)
                        ORDER BY id
                        FOR UPDATE;
                    END IF;

                    IF TG_OP IN ('UPDATE', 'DELETE')
                       AND EXISTS (
                            SELECT 1
                            FROM knowledge_releases
                            WHERE id = OLD.release_id AND published_at IS NOT NULL
                       ) THEN
                        RAISE EXCEPTION 'Published knowledge release children are immutable.'
                            USING ERRCODE = 'P0001';
                    END IF;

                    IF TG_OP IN ('INSERT', 'UPDATE')
                       AND EXISTS (
                            SELECT 1
                            FROM knowledge_releases
                            WHERE id = NEW.release_id AND published_at IS NOT NULL
                       ) THEN
                        RAISE EXCEPTION 'Published knowledge release children are immutable.'
                            USING ERRCODE = 'P0001';
                    END IF;

                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_guard_resource_revision_release
                    BEFORE INSERT OR UPDATE OR DELETE ON resource_revisions
                    FOR EACH ROW
                    EXECUTE FUNCTION guard_published_release_child();

                CREATE TRIGGER trg_guard_knowledge_release_manifest_release
                    BEFORE INSERT OR UPDATE OR DELETE ON knowledge_release_manifests
                    FOR EACH ROW
                    EXECUTE FUNCTION guard_published_release_child();

                CREATE FUNCTION guard_current_knowledge_release()
                RETURNS trigger AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'The current knowledge release pointer cannot be deleted.'
                            USING ERRCODE = 'P0001';
                    END IF;

                    IF current_setting('jp_reading.controlled_publication', TRUE) IS DISTINCT FROM 'on' THEN
                        RAISE EXCEPTION 'The current knowledge release may only change during controlled publication.'
                            USING ERRCODE = 'P0001';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM knowledge_releases
                        WHERE id = NEW.release_id AND published_at IS NOT NULL
                    ) THEN
                        RAISE EXCEPTION 'Only a published knowledge release can become current.'
                            USING ERRCODE = 'P0001';
                    END IF;

                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_guard_current_knowledge_release
                    BEFORE INSERT OR UPDATE OR DELETE ON current_knowledge_release
                    FOR EACH ROW
                    EXECUTE FUNCTION guard_current_knowledge_release();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_guard_current_knowledge_release ON current_knowledge_release;
                DROP FUNCTION IF EXISTS guard_current_knowledge_release();
                DROP TRIGGER IF EXISTS trg_guard_resource_revision_release ON resource_revisions;
                DROP TRIGGER IF EXISTS trg_guard_knowledge_release_manifest_release ON knowledge_release_manifests;
                DROP FUNCTION IF EXISTS guard_published_release_child();
                DROP TRIGGER IF EXISTS trg_guard_published_knowledge_release ON knowledge_releases;
                DROP FUNCTION IF EXISTS guard_published_knowledge_release();
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_knowledge_release_manifests_knowledge_releases_release_id",
                table: "knowledge_release_manifests");

            migrationBuilder.DropForeignKey(
                name: "FK_resource_revisions_knowledge_releases_release_id",
                table: "resource_revisions");

            migrationBuilder.AddColumn<bool>(
                name: "is_current",
                table: "knowledge_releases",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE knowledge_releases AS releases
                SET is_current = TRUE
                FROM current_knowledge_release AS current_release
                WHERE releases.id = current_release.release_id;
                """);

            migrationBuilder.DropTable(
                name: "current_knowledge_release");

            migrationBuilder.CreateIndex(
                name: "uq_knowledge_releases_current",
                table: "knowledge_releases",
                column: "is_current",
                unique: true,
                filter: "is_current = TRUE");

            migrationBuilder.AddForeignKey(
                name: "FK_knowledge_release_manifests_knowledge_releases_release_id",
                table: "knowledge_release_manifests",
                column: "release_id",
                principalTable: "knowledge_releases",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_resource_revisions_knowledge_releases_release_id",
                table: "resource_revisions",
                column: "release_id",
                principalTable: "knowledge_releases",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
