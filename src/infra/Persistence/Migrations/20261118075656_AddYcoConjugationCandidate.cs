using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using Infrastructure.Conjugation;

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddYcoConjugationCandidate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Install the reviewed YCO snapshot as a new immutable version beside candidate-20261013.1,
            // so operators can pin either version and roll back by configuration only.
            migrationBuilder.InsertData(table: "conjugation_datasets", columns: new[] { "version", "content" },
                values: new object[] { YcoConjugationCandidateData.Version, YcoConjugationCandidateData.ReadContent() },
                columnTypes: new[] { "text", "jsonb" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Conjugation datasets are immutable; use a forward migration.");
        }
    }
}
