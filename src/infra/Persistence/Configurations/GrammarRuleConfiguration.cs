using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class GrammarRuleConfiguration : IEntityTypeConfiguration<GrammarRule>
{
    public void Configure(EntityTypeBuilder<GrammarRule> builder)
    {
        builder.ToTable("grammar_rules");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CanonicalId).HasColumnName("canonical_id").IsRequired();
        builder.Property(e => e.Pattern).HasColumnName("pattern").IsRequired();
        builder.Property(e => e.JlptLevel).HasColumnName("jlpt_level");
        builder.Property(e => e.MeaningVi).HasColumnName("meaning_vi");
        builder.Property(e => e.MeaningEn).HasColumnName("meaning_en");
        builder.Property(e => e.Formation).HasColumnName("formation");
        builder.Property(e => e.Examples).HasColumnName("examples").HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        builder.Property(e => e.MatcherMetadata).HasColumnName("matcher_metadata").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.Notes).HasColumnName("notes");
        builder.Property(e => e.SourceMetadata).HasColumnName("source_metadata").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => e.CanonicalId).IsUnique().HasDatabaseName("uq_grammar_rules_canonical_id");
        builder.HasIndex(e => e.JlptLevel).HasDatabaseName("idx_grammar_rules_jlpt").HasFilter("jlpt_level IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_grammar_rules_jlpt",
            "jlpt_level IS NULL OR jlpt_level IN ('N5', 'N4', 'N3', 'N2', 'N1')"));
    }
}
