using Microsoft.EntityFrameworkCore.Migrations;

using Infrastructure.Conjugation;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConjugationDatasets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conjugation_datasets",
                columns: table => new
                {
                    version = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conjugation_datasets", x => x.version);
                });
            // EARS[Ubiquitous]: Knowledge is data, installed as an immutable versioned document.
            migrationBuilder.InsertData(table: "conjugation_datasets", columns: new[] { "version", "content" },
                values: new object[] { ConjugationSeedData.DevelopmentVersion, ConjugationSeedData.ReadDevelopmentContent() },
                columnTypes: new[] { "text", "jsonb" });
            migrationBuilder.Sql("""
                CREATE FUNCTION reject_conjugation_dataset_mutation() RETURNS trigger
                LANGUAGE plpgsql AS $$ BEGIN
                    RAISE EXCEPTION 'Conjugation datasets are immutable; install a new version';
                END $$;
                CREATE TRIGGER conjugation_dataset_immutable
                BEFORE UPDATE OR DELETE ON conjugation_datasets
                FOR EACH ROW EXECUTE FUNCTION reject_conjugation_dataset_mutation();
                CREATE TRIGGER conjugation_dataset_no_truncate
                BEFORE TRUNCATE ON conjugation_datasets
                FOR EACH STATEMENT EXECUTE FUNCTION reject_conjugation_dataset_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conjugation_datasets");
            migrationBuilder.Sql("DROP FUNCTION reject_conjugation_dataset_mutation();");
        }
    }
}
