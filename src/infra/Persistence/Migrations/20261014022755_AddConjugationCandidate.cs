using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using Infrastructure.Conjugation;

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConjugationCandidate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EARS[Ubiquitous] F09 FR-006/009: Install a new immutable version; never replace old knowledge.
            migrationBuilder.InsertData(table: "conjugation_datasets", columns: new[] { "version", "content" },
                values: new object[] { ConjugationCandidateData.Version, ConjugationCandidateData.ReadContent() },
                columnTypes: new[] { "text", "jsonb" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Conjugation datasets are immutable; use a forward migration.");
        }
    }
}
