using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class KanjiRecordConfiguration : IEntityTypeConfiguration<KanjiRecord>
{
    public void Configure(EntityTypeBuilder<KanjiRecord> builder)
    {
        builder.ToTable("kanji_records");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.CanonicalId).HasColumnName("canonical_id").IsRequired();
        builder.Property(e => e.Character).HasColumnName("character").IsRequired();
        builder.Property(e => e.StrokeCount).HasColumnName("stroke_count");
        builder.Property(e => e.Grade).HasColumnName("grade");
        builder.Property(e => e.JlptLevel).HasColumnName("jlpt_level");
        builder.Property(e => e.JlptProvenance).HasColumnName("jlpt_provenance");
        builder.Property(e => e.Frequency).HasColumnName("frequency");
        builder.Property(e => e.UnicodeCodepoint).HasColumnName("unicode_codepoint").IsRequired();
        builder.Property(e => e.RadicalNumber).HasColumnName("radical_number");
        builder.Property(e => e.HanViet).HasColumnName("han_viet");
        builder.Property(e => e.OnReadings).HasColumnName("on_readings");
        builder.Property(e => e.KunReadings).HasColumnName("kun_readings");
        builder.Property(e => e.MeaningsEn).HasColumnName("meanings_en");
        builder.Property(e => e.MeaningsVi).HasColumnName("meanings_vi");
        builder.Property(e => e.NanoriReadings).HasColumnName("nanori_readings");
        builder.Property(e => e.SourceMetadata).HasColumnName("source_metadata").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasIndex(e => e.CanonicalId).IsUnique().HasDatabaseName("uq_kanji_records_canonical_id");
        builder.HasIndex(e => e.Character).IsUnique().HasDatabaseName("uq_kanji_records_character");
        builder.HasIndex(e => e.JlptLevel).HasDatabaseName("idx_kanji_records_jlpt").HasFilter("jlpt_level IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_kanji_records_jlpt_provenance",
            "jlpt_provenance IS NULL OR jlpt_provenance IN ('authoritative', 'derived', 'approximate')"));
    }
}
